﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding;
using Content.Server.GameTicking.GamePresets;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.Chat;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.Jobs;
using Content.Shared.Preferences;
using Prometheus;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameTicking
{
    public partial class GameTicker : SharedGameTicker, IGameTicker
    {
        private static readonly Counter RoundNumberMetric = Metrics.CreateCounter(
            "ss14_round_number",
            "Round number.");

        private static readonly Gauge RoundLengthMetric = Metrics.CreateGauge(
            "ss14_round_length",
            "Round length in seconds.");

        private static readonly TimeSpan UpdateRestartDelay = TimeSpan.FromSeconds(20);

        private const string PlayerPrototypeName = "HumanMob_Content";
        private const string ObserverPrototypeName = "MobObserver";
        private const string MapFile = "Maps/saltern.yml";
        private static TimeSpan _roundStartTimeSpan;

        [ViewVariables] private readonly List<GameRule> _gameRules = new List<GameRule>();
        [ViewVariables] private readonly List<ManifestEntry> _manifest = new List<ManifestEntry>();

        // Value is whether they're ready.
        [ViewVariables]
        private readonly Dictionary<IPlayerSession, bool> _playersInLobby = new Dictionary<IPlayerSession, bool>();

        [ViewVariables] private bool _initialized;

        [ViewVariables] private Type _presetType;

        [ViewVariables] private DateTime _pauseTime;
        [ViewVariables] private bool _roundStartCountdownHasNotStartedYetDueToNoPlayers;
        private DateTime _roundStartTimeUtc;
        [ViewVariables] private GameRunLevel _runLevel;
        [ViewVariables(VVAccess.ReadWrite)] private GridCoordinates _spawnPoint;

        [ViewVariables] private bool LobbyEnabled => _configurationManager.GetCVar<bool>("game.lobbyenabled");

        [ViewVariables] private bool _updateOnRoundEnd;
        private CancellationTokenSource _updateShutdownCts;


        [ViewVariables] public bool Paused { get; private set; }

        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                if (_runLevel == value) return;

                var old = _runLevel;
                _runLevel = value;

                OnRunLevelChanged?.Invoke(new GameRunLevelChangedEventArgs(old, value));
            }
        }

        public event Action<GameRunLevelChangedEventArgs> OnRunLevelChanged;
        public event Action<GameRuleAddedEventArgs> OnRuleAdded;

        private TimeSpan LobbyDuration =>
            TimeSpan.FromSeconds(_configurationManager.GetCVar<int>("game.lobbyduration"));

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _configurationManager.RegisterCVar("game.lobbyenabled", false, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("game.lobbyduration", 20, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("game.defaultpreset", "Suspicion", CVar.ARCHIVE);
            _configurationManager.RegisterCVar("game.fallbackpreset", "Sandbox", CVar.ARCHIVE);

            _playerManager.PlayerStatusChanged += _handlePlayerStatusChanged;

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby));
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame));
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus));
            _netManager.RegisterNetMessage<MsgTickerLobbyInfo>(nameof(MsgTickerLobbyInfo));
            _netManager.RegisterNetMessage<MsgTickerLobbyCountdown>(nameof(MsgTickerLobbyCountdown));
            _netManager.RegisterNetMessage<MsgRoundEndMessage>(nameof(MsgRoundEndMessage));

            SetStartPreset(_configurationManager.GetCVar<string>("game.defaultpreset"));

            RestartRound();

            _initialized = true;

            JobControllerInit();

            _watchdogApi.UpdateReceived += WatchdogApiOnUpdateReceived;
        }

        private void WatchdogApiOnUpdateReceived()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString(
                "Update has been received, server will automatically restart for update at the end of this round."));
            _updateOnRoundEnd = true;
            ServerEmptyUpdateRestartCheck();
        }

        public void Update(FrameEventArgs frameEventArgs)
        {
            if (RunLevel == GameRunLevel.InRound)
            {
                RoundLengthMetric.Inc(frameEventArgs.DeltaSeconds);
            }

            if (RunLevel != GameRunLevel.PreRoundLobby ||
                Paused ||
                _roundStartTimeUtc > DateTime.UtcNow ||
                _roundStartCountdownHasNotStartedYetDueToNoPlayers)
            {
                return;
            }

            StartRound();
        }

        public void RestartRound()
        {
            if (_updateOnRoundEnd)
            {
                _baseServer.Shutdown(
                    Loc.GetString("Server is shutting down for update and will automatically restart."));
                return;
            }

            Logger.InfoS("ticker", "Restarting round!");

            SendServerMessage("Restarting round...");

            RoundNumberMetric.Inc();

            RunLevel = GameRunLevel.PreRoundLobby;
            _resettingCleanup();
            _preRoundSetup();

            if (!LobbyEnabled)
            {
                StartRound();
            }
            else
            {
                if (_playerManager.PlayerCount == 0)
                    _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
                else
                    _roundStartTimeUtc = DateTime.UtcNow + LobbyDuration;

                _sendStatusToAll();
            }
        }

        public void StartRound(bool force = false)
        {
            DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
            Logger.InfoS("ticker", "Starting round!");

            SendServerMessage("The round is starting now...");

            List<IPlayerSession> readyPlayers;
            if (LobbyEnabled)
            {
                readyPlayers = _playersInLobby.Where(p => p.Value).Select(p => p.Key).ToList();
            }
            else
            {
                readyPlayers = _playersInLobby.Keys.ToList();
            }

            RunLevel = GameRunLevel.InRound;

            RoundLengthMetric.Set(0);

            // Get the profiles for each player for easier lookup.
            var profiles = readyPlayers.ToDictionary(p => p, GetPlayerProfile);

            var assignedJobs = AssignJobs(readyPlayers, profiles);

            // For players without jobs, give them the overflow job if they have that set...
            foreach (var player in readyPlayers)
            {
                if (assignedJobs.ContainsKey(player))
                {
                    continue;
                }

                var profile = profiles[player];
                if (profile.PreferenceUnavailable == PreferenceUnavailableMode.SpawnAsOverflow)
                {
                    assignedJobs.Add(player, OverflowJob);
                }
            }

            // Spawn everybody in!
            foreach (var (player, job) in assignedJobs)
            {
                SpawnPlayer(player, job, false);
            }

            // Time to start the preset.
            var preset = MakeGamePreset();

            if (!preset.Start(assignedJobs.Keys.ToList(), force))
            {
                SetStartPreset(_configurationManager.GetCVar<string>("game.fallbackpreset"));
                var newPreset = MakeGamePreset();
                _chatManager.DispatchServerAnnouncement($"Failed to start {preset.ModeTitle} mode! Defaulting to {newPreset.ModeTitle}...");
                if (!newPreset.Start(readyPlayers, force))
                {
                    throw new ApplicationException("Fallback preset failed to start!");
                }
            }

            _roundStartTimeSpan = IoCManager.Resolve<IGameTiming>().RealTime;
            _sendStatusToAll();
        }

        private void SendServerMessage(string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            IoCManager.Resolve<IServerNetManager>().ServerSendToAll(msg);
        }

        private HumanoidCharacterProfile GetPlayerProfile(IPlayerSession p) =>
            (HumanoidCharacterProfile) _prefsManager.GetPreferences(p.SessionId.Username).SelectedCharacter;

        public void EndRound()
        {
            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            Logger.InfoS("ticker", "Ending round!");

            RunLevel = GameRunLevel.PostRound;

            //Tell every client the round has ended.
            var roundEndMessage = _netManager.CreateNetMessage<MsgRoundEndMessage>();
            roundEndMessage.GamemodeTitle = MakeGamePreset().ModeTitle;

            //Get the timespan of the round.
            roundEndMessage.RoundDuration = IoCManager.Resolve<IGameTiming>().RealTime.Subtract(_roundStartTimeSpan);

            //Generate a list of basic player info to display in the end round summary.
            var listOfPlayerInfo = new List<RoundEndPlayerInfo>();
            foreach(var ply in _playerManager.GetAllPlayers().OrderBy(p => p.Name))
            {
                var mind = ply.ContentData().Mind;
                if(mind != null)
                {
                    var antag = mind.AllRoles.Any(role => role.Antag);
                    var playerEndRoundInfo = new RoundEndPlayerInfo()
                    {
                        PlayerOOCName = ply.Name,
                        PlayerICName = mind.CurrentEntity.Name,
                        Role = antag ? mind.AllRoles.First(role => role.Antag).Name : mind.AllRoles.FirstOrDefault()?.Name ?? Loc.GetString("Unknown"),
                        Antag = antag
                    };
                    listOfPlayerInfo.Add(playerEndRoundInfo);
                }
            }

            roundEndMessage.AllPlayersEndInfo = listOfPlayerInfo;
            _netManager.ServerSendToAll(roundEndMessage);
        }

        public void Respawn(IPlayerSession targetPlayer)
        {
            targetPlayer.ContentData().WipeMind();

            if (LobbyEnabled)
                _playerJoinLobby(targetPlayer);
            else
                SpawnPlayer(targetPlayer);
        }

        public void MakeObserve(IPlayerSession player)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            _spawnObserver(player);
        }

        public void MakeJoinGame(IPlayerSession player)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            SpawnPlayer(player);
        }

        public void ToggleReady(IPlayerSession player, bool ready)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            _playersInLobby[player] = ready;
            _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
        }

        public T AddGameRule<T>() where T : GameRule, new()
        {
            var instance = _dynamicTypeFactory.CreateInstance<T>();

            _gameRules.Add(instance);
            instance.Added();

            OnRuleAdded?.Invoke(new GameRuleAddedEventArgs(instance));

            return instance;
        }

        public bool HasGameRule(Type t)
        {
            if (t == null || !t.IsAssignableFrom(typeof(GameRule)))
                return false;

            foreach (var rule in _gameRules)
            {
                if (rule.GetType().Equals(t))
                    return true;
            }

            return false;
        }

        public void RemoveGameRule(GameRule rule)
        {
            if (_gameRules.Contains(rule)) return;

            rule.Removed();

            _gameRules.Remove(rule);
        }

        public IEnumerable<GameRule> ActiveGameRules => _gameRules;

        public bool TryGetPreset(string name, out Type type)
        {
            type = name.ToLower() switch
            {
                "sandbox" => typeof(PresetSandbox),
                "deathmatch" => typeof(PresetDeathMatch),
                "suspicion" => typeof(PresetSuspicion),
                _ => default
            };

            return type != default;
        }

        public void SetStartPreset(Type type, bool force = false)
        {
            if (!typeof(GamePreset).IsAssignableFrom(type)) throw new ArgumentException("type must inherit GamePreset");

            _presetType = type;
            UpdateInfoText();

            if (force)
            {
                StartRound(true);
            }
        }

        public void SetStartPreset(string name, bool force = false)
        {
            if (!TryGetPreset(name, out var type))
            {
                throw new NotSupportedException();
            }

            SetStartPreset(type, force);
        }

        public bool DelayStart(TimeSpan time)
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
            {
                return false;
            }

            _roundStartTimeUtc += time;

            var lobbyCountdownMessage = _netManager.CreateNetMessage<MsgTickerLobbyCountdown>();
            lobbyCountdownMessage.StartTime = _roundStartTimeUtc;
            lobbyCountdownMessage.Paused = Paused;
            _netManager.ServerSendToAll(lobbyCountdownMessage);

            _chatManager.DispatchServerAnnouncement($"Round start has been delayed for {time.TotalSeconds} seconds.");

            return true;
        }

        public bool PauseStart(bool pause = true)
        {
            if (Paused == pause)
            {
                return false;
            }

            Paused = pause;

            if (pause)
            {
                _pauseTime = DateTime.UtcNow;
            }
            else if (_pauseTime != default)
            {
                _roundStartTimeUtc += DateTime.UtcNow - _pauseTime;
            }

            var lobbyCountdownMessage = _netManager.CreateNetMessage<MsgTickerLobbyCountdown>();
            lobbyCountdownMessage.StartTime = _roundStartTimeUtc;
            lobbyCountdownMessage.Paused = Paused;
            _netManager.ServerSendToAll(lobbyCountdownMessage);

            _chatManager.DispatchServerAnnouncement(Paused
                ? "Round start has been paused."
                : "Round start countdown is now resumed.");

            return true;
        }

        public bool TogglePause()
        {
            PauseStart(!Paused);
            return Paused;
        }

        private IEntity _spawnPlayerMob(Job job, bool lateJoin = true)
        {
            GridCoordinates coordinates = lateJoin ? GetLateJoinSpawnPoint() : GetJobSpawnPoint(job.Prototype.ID);
            var entity = _entityManager.SpawnEntity(PlayerPrototypeName, coordinates);
            var startingGear = _prototypeManager.Index<StartingGearPrototype>(job.StartingGear);
            if (entity.TryGetComponent(out InventoryComponent inventory))
            {
                var gear = startingGear.Equipment;

                foreach (var (slot, equipmentStr) in gear)
                {
                    var equipmentEntity = _entityManager.SpawnEntity(equipmentStr, entity.Transform.GridPosition);
                    inventory.Equip(slot, equipmentEntity.GetComponent<ItemComponent>());
                }
            }

            if (entity.TryGetComponent(out HandsComponent handsComponent))
            {
                var inhand = startingGear.Inhand;
                foreach (var (hand, prototype) in inhand)
                {
                    var inhandEntity = _entityManager.SpawnEntity(prototype, entity.Transform.GridPosition);
                    handsComponent.PutInHand(inhandEntity.GetComponent<ItemComponent>(), hand);
                }
            }

            return entity;
        }

        private void ApplyCharacterProfile(IEntity entity, ICharacterProfile profile)
        {
            if (profile is null)
                return;
            entity.GetComponent<HumanoidAppearanceComponent>().UpdateFromProfile(profile);
            entity.Name = profile.Name;
        }

        private IEntity _spawnObserverMob()
        {
            var coordinates = GetObserverSpawnPoint();
            return _entityManager.SpawnEntity(ObserverPrototypeName, coordinates);
        }

        public GridCoordinates GetLateJoinSpawnPoint()
        {
            var location = _spawnPoint;

            var possiblePoints = new List<GridCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.LateJoin) possiblePoints.Add(entity.Transform.GridPosition);
            }

            if (possiblePoints.Count != 0) location = _robustRandom.Pick(possiblePoints);

            return location;
        }

        public GridCoordinates GetJobSpawnPoint(string jobId)
        {
            var location = _spawnPoint;

            var possiblePoints = new List<GridCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.Job && point.Job.ID == jobId)
                    possiblePoints.Add(entity.Transform.GridPosition);
            }

            if (possiblePoints.Count != 0) location = _robustRandom.Pick(possiblePoints);

            return location;
        }

        public GridCoordinates GetObserverSpawnPoint()
        {
            var location = _spawnPoint;

            var possiblePoints = new List<GridCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.Observer)
                    possiblePoints.Add(entity.Transform.GridPosition);
            }

            if (possiblePoints.Count != 0) location = _robustRandom.Pick(possiblePoints);

            return location;
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void _resettingCleanup()
        {
            // Delete all entities.
            foreach (var entity in _entityManager.GetEntities().ToList())
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                entity.Delete();

            _mapManager.Restart();

            // Delete the minds of everybody.
            // TODO: Maybe move this into a separate manager?
            foreach (var unCastData in _playerManager.GetAllPlayerData()) unCastData.ContentData().WipeMind();

            // Clear up any game rules.
            foreach (var rule in _gameRules) rule.Removed();

            _gameRules.Clear();

            // Move everybody currently in the server to lobby.
            foreach (var player in _playerManager.GetAllPlayers())
            {
                if (_playersInLobby.ContainsKey(player)) continue;

                _playerJoinLobby(player);
            }

            // Reset pathing system
            EntitySystem.Get<PathfindingSystem>().ResettingCleanup();

            _spawnedPositions.Clear();
            _manifest.Clear();

            EntitySystem.Get<WireHackingSystem>().ResetLayouts();
        }

        private void _preRoundSetup()
        {
            var newMapId = _mapManager.CreateMap();
            var startTime = _gameTiming.RealTime;
            var grid = _mapLoader.LoadBlueprint(newMapId, MapFile);

            _spawnPoint = new GridCoordinates(Vector2.Zero, grid);

            var timeSpan = _gameTiming.RealTime - startTime;
            Logger.InfoS("ticker", $"Loaded map in {timeSpan.TotalMilliseconds:N2}ms.");
        }

        private void _handlePlayerStatusChanged(object sender, SessionStatusEventArgs args)
        {
            var session = args.Session;

            switch (args.NewStatus)
            {
                case SessionStatus.Connecting:
                    // Cancel shutdown update timer in progress.
                    _updateShutdownCts?.Cancel();
                    break;

                case SessionStatus.Connected:
                {
                    // Always make sure the client has player data. Mind gets assigned on spawn.
                    if (session.Data.ContentDataUncast == null)
                        session.Data.ContentDataUncast = new PlayerData(session.SessionId);

                    // timer time must be > tick length
                    Timer.Spawn(0, args.Session.JoinGame);

                    _chatManager.DispatchServerAnnouncement($"Player {args.Session.SessionId} joined server!");

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTimeUtc = DateTime.UtcNow + LobbyDuration;
                    }

                    break;
                }

                case SessionStatus.InGame:
                {
                    var data = session.ContentData();
                    if (data.Mind == null)
                    {
                        if (LobbyEnabled)
                        {
                            _playerJoinLobby(session);
                            return;
                        }

                        SpawnPlayer(session);
                    }
                    else
                    {
                        if (data.Mind.CurrentEntity == null)
                        {
                            SpawnPlayer(session);
                        }
                        else
                        {
                            session.AttachToEntity(data.Mind.CurrentEntity);
                            _playerJoinGame(session);
                        }
                    }

                    break;
                }

                case SessionStatus.Disconnected:
                {
                    if (_playersInLobby.ContainsKey(session)) _playersInLobby.Remove(session);

                    _chatManager.DispatchServerAnnouncement($"Player {args.Session.SessionId} left server!");
                    ServerEmptyUpdateRestartCheck();
                    break;
                }
            }
        }

        /// <summary>
        ///     Checks whether there are still players on the server,
        /// and if not starts a timer to automatically reboot the server if an update is available.
        /// </summary>
        private void ServerEmptyUpdateRestartCheck()
        {
            // Can't simple check the current connected player count since that doesn't update
            // before PlayerStatusChanged gets fired.
            // So in the disconnect handler we'd still see a single player otherwise.
            var playersOnline = _playerManager.GetAllPlayers().Any(p => p.Status != SessionStatus.Disconnected);
            if (playersOnline || !_updateOnRoundEnd)
            {
                // Still somebody online.
                return;
            }

            if (_updateShutdownCts != null && !_updateShutdownCts.IsCancellationRequested)
            {
                // Do nothing because I guess we already have a timer running..?
                return;
            }

            _updateShutdownCts = new CancellationTokenSource();

            Timer.Spawn(UpdateRestartDelay, () =>
            {
                _baseServer.Shutdown(
                    Loc.GetString("Server is shutting down for update and will automatically restart."));
            }, _updateShutdownCts.Token);
        }

        private void SpawnPlayer(IPlayerSession session, string jobId = null, bool lateJoin = true)
        {
            var character = (HumanoidCharacterProfile) _prefsManager
                .GetPreferences(session.SessionId.Username)
                .SelectedCharacter;

            _playerJoinGame(session);

            var data = session.ContentData();
            data.WipeMind();
            data.Mind = new Mind(session.SessionId)
            {
                CharacterName = character.Name
            };

            if (jobId == null)
            {
                // Pick best job best on prefs.
                jobId = PickBestAvailableJob(character);
            }

            var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);
            var job = new Job(data.Mind, jobPrototype);
            data.Mind.AddRole(job);

            var mob = _spawnPlayerMob(job, lateJoin);
            data.Mind.TransferTo(mob);
            ApplyCharacterProfile(mob, character);

            AddManifestEntry(character.Name, jobId);
            AddSpawnedPosition(jobId);
            EquipIdCard(mob, character.Name, jobPrototype);
        }

        private void EquipIdCard(IEntity mob, string characterName, JobPrototype jobPrototype)
        {
            var inventory = mob.GetComponent<InventoryComponent>();

            if (!inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent pdaItem))
            {
                return;
            }

            var pda = pdaItem.Owner;

            var pdaComponent = pda.GetComponent<PDAComponent>();
            if (pdaComponent.IdSlotEmpty)
            {
                return;
            }

            var card = pdaComponent.ContainedID;
            card.FullName = characterName;
            card.JobTitle = jobPrototype.Name;

            var access = card.Owner.GetComponent<AccessComponent>();
            var accessTags = access.Tags;
            accessTags.UnionWith(jobPrototype.Access);
            pdaComponent.SetPDAOwner(mob);
            var mindComponent = mob.GetComponent<MindComponent>();
            if (mindComponent.HasMind)//Redundancy checks.
            {
                if (mindComponent.Mind.AllRoles.Any(role => role.Antag)) //Give antags a new uplinkaccount.
                {
                    var uplinkAccount = new UplinkAccount(mob.Uid, 20); //TODO: make me into a variable based on server pop or something.
                    pdaComponent.InitUplinkAccount(uplinkAccount);
                }
            }

        }

        private void AddManifestEntry(string characterName, string jobId)
        {
            _manifest.Add(new ManifestEntry(characterName, jobId));
        }

        private void _spawnObserver(IPlayerSession session)
        {
            var name = _prefsManager
                .GetPreferences(session.SessionId.Username)
                .SelectedCharacter.Name;

            _playerJoinGame(session);
            var data = session.ContentData();
            data.WipeMind();
            data.Mind = new Mind(session.SessionId);

            var mob = _spawnObserverMob();
            mob.Name = name;
            mob.GetComponent<GhostComponent>().CanReturnToBody = false;
            data.Mind.TransferTo(mob);
        }

        private void _playerJoinLobby(IPlayerSession session)
        {
            _playersInLobby.Add(session, false);

            _prefsManager.OnClientConnected(session);
            _netManager.ServerSendMessage(_netManager.CreateNetMessage<MsgTickerJoinLobby>(), session.ConnectedClient);
            _netManager.ServerSendMessage(_getStatusMsg(session), session.ConnectedClient);
            _netManager.ServerSendMessage(GetInfoMsg(), session.ConnectedClient);
        }

        private void _playerJoinGame(IPlayerSession session)
        {
            _chatManager.DispatchServerMessage(session,
                "Welcome to Space Station 14! If this is your first time checking out the game, be sure to check out the tutorial in the top left!");
            if (_playersInLobby.ContainsKey(session)) _playersInLobby.Remove(session);

            _netManager.ServerSendMessage(_netManager.CreateNetMessage<MsgTickerJoinGame>(), session.ConnectedClient);
        }

        private MsgTickerLobbyStatus _getStatusMsg(IPlayerSession session)
        {
            _playersInLobby.TryGetValue(session, out var ready);
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyStatus>();
            msg.IsRoundStarted = RunLevel != GameRunLevel.PreRoundLobby;
            msg.StartTime = _roundStartTimeUtc;
            msg.YouAreReady = ready;
            msg.Paused = Paused;
            return msg;
        }

        private MsgTickerLobbyInfo GetInfoMsg()
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyInfo>();
            msg.TextBlob = GetInfoText();
            return msg;
        }

        private void _sendStatusToAll()
        {
            foreach (var player in _playersInLobby.Keys)
                _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
        }

        private string GetInfoText()
        {
            var gmTitle = MakeGamePreset().ModeTitle;
            var desc = MakeGamePreset().Description;
            return _localization.GetString(@"Hi and welcome to [color=white]Space Station 14![/color]

The current game mode is: [color=white]{0}[/color].
[color=yellow]{1}[/color]", gmTitle, desc );
        }

        private void UpdateInfoText()
        {
            var infoMsg = GetInfoMsg();

            _netManager.ServerSendToMany(infoMsg, _playersInLobby.Keys.Select(p => p.ConnectedClient).ToList());
        }

        private GamePreset MakeGamePreset()
        {
            return _dynamicTypeFactory.CreateInstance<GamePreset>(_presetType ?? typeof(PresetSandbox));
        }

#pragma warning disable 649
        [Dependency] private IEntityManager _entityManager;
        [Dependency] private IMapManager _mapManager;
        [Dependency] private IMapLoader _mapLoader;
        [Dependency] private IGameTiming _gameTiming;
        [Dependency] private IConfigurationManager _configurationManager;
        [Dependency] private IPlayerManager _playerManager;
        [Dependency] private IChatManager _chatManager;
        [Dependency] private IServerNetManager _netManager;
        [Dependency] private IDynamicTypeFactory _dynamicTypeFactory;
        [Dependency] private IPrototypeManager _prototypeManager;
        [Dependency] private readonly ILocalizationManager _localization;
        [Dependency] private readonly IRobustRandom _robustRandom;
        [Dependency] private readonly IServerPreferencesManager _prefsManager;
        [Dependency] private readonly IBaseServer _baseServer;
        [Dependency] private readonly IWatchdogApi _watchdogApi;
#pragma warning restore 649
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound = 1,
        PostRound = 2
    }

    public class GameRunLevelChangedEventArgs : EventArgs
    {
        public GameRunLevelChangedEventArgs(GameRunLevel oldRunLevel, GameRunLevel newRunLevel)
        {
            OldRunLevel = oldRunLevel;
            NewRunLevel = newRunLevel;
        }

        public GameRunLevel OldRunLevel { get; }
        public GameRunLevel NewRunLevel { get; }
    }

    public class GameRuleAddedEventArgs : EventArgs
    {
        public GameRule GameRule { get; }

        public GameRuleAddedEventArgs(GameRule rule)
        {
            GameRule = rule;
        }
    }
}
