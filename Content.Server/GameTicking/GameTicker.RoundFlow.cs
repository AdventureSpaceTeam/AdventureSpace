using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost;
using Content.Server.Maps;
using Content.Server.Mind;
using Content.Server.Players;
using Content.Server.Station;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Station;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        private static readonly Counter RoundNumberMetric = Metrics.CreateCounter(
            "ss14_round_number",
            "Round number.");

        private static readonly Gauge RoundLengthMetric = Metrics.CreateGauge(
            "ss14_round_length",
            "Round length in seconds.");

#if EXCEPTION_TOLERANCE
        [ViewVariables]
        private int _roundStartFailCount = 0;
#endif

        [ViewVariables]
        private TimeSpan _roundStartTimeSpan;

        [ViewVariables]
        private bool _startingRound;

        [ViewVariables]
        private GameRunLevel _runLevel;

        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                // Game admins can run `restartroundnow` while still in-lobby, which'd break things with this check.
                // if (_runLevel == value) return;

                var old = _runLevel;
                _runLevel = value;

                RaiseLocalEvent(new GameRunLevelChangedEvent(old, value));
            }
        }

        [ViewVariables]
        public int RoundId { get; private set; }

        private void LoadMaps()
        {
            AddGamePresetRules();

            DefaultMap = _mapManager.CreateMap();
            _mapManager.AddUninitializedMap(DefaultMap);
            var startTime = _gameTiming.RealTime;
            var maps = new List<GameMapPrototype>() { _gameMapManager.GetSelectedMapChecked(true) };

            // Let game rules dictate what maps we should load.
            RaiseLocalEvent(new LoadingMapsEvent(maps));

            foreach (var map in maps)
            {
                var toLoad = DefaultMap;
                if (maps[0] != map)
                {
                    // Create other maps for the others since we need to.
                    toLoad = _mapManager.CreateMap();
                    _mapManager.AddUninitializedMap(toLoad);
                }

                _mapLoader.LoadMap(toLoad, map.MapPath.ToString());

                var grids = _mapManager.GetAllMapGrids(toLoad).ToList();
                var dict = new Dictionary<string, StationId>();

                StationId SetupInitialStation(IMapGrid grid, GameMapPrototype map)
                {
                    var stationId = _stationSystem.InitialSetupStationGrid(grid.GridEntityId, map);
                    SetupGridStation(grid);

                    // ass!
                    _spawnPoint = grid.ToCoordinates();
                    return stationId;
                }

                // Iterate over all BecomesStation
                for (var i = 0; i < grids.Count; i++)
                {
                    var grid = grids[i];

                    // We still setup the grid
                    if (!TryComp<BecomesStationComponent>(grid.GridEntityId, out var becomesStation))
                        continue;

                    var stationId = SetupInitialStation(grid, map);

                    dict.Add(becomesStation.Id, stationId);
                }

                if (!dict.Any())
                {
                    // Oh jeez, no stations got loaded.
                    // We'll just take the first grid and setup that, then.

                    var grid = grids[0];
                    var stationId = SetupInitialStation(grid, map);

                    dict.Add("Station", stationId);
                }

                // Iterate over all PartOfStation
                for (var i = 0; i < grids.Count; i++)
                {
                    var grid = grids[i];
                    if (!TryComp<PartOfStationComponent>(grid.GridEntityId, out var partOfStation))
                        continue;
                    SetupGridStation(grid);

                    if (dict.TryGetValue(partOfStation.Id, out var stationId))
                    {
                        _stationSystem.AddGridToStation(grid.GridEntityId, stationId);
                    }
                    else
                    {
                        Logger.Error($"Grid {grid.Index} ({grid.GridEntityId}) specified that it was part of station {partOfStation.Id} which does not exist");
                    }
                }
            }

            var timeSpan = _gameTiming.RealTime - startTime;
            Logger.InfoS("ticker", $"Loaded maps in {timeSpan.TotalMilliseconds:N2}ms.");
        }

        private void SetupGridStation(IMapGrid grid)
        {
            var stationXform = EntityManager.GetComponent<TransformComponent>(grid.GridEntityId);

            if (StationOffset)
            {
                // Apply a random offset to the station grid entity.
                var x = _robustRandom.NextFloat(-MaxStationOffset, MaxStationOffset);
                var y = _robustRandom.NextFloat(-MaxStationOffset, MaxStationOffset);
                stationXform.LocalPosition = new Vector2(x, y);
            }

            if (StationRotation)
            {
                stationXform.LocalRotation = _robustRandom.NextFloat(MathF.Tau);
            }
        }

        public void StartRound(bool force = false)
        {
#if EXCEPTION_TOLERANCE
            try
            {
#endif
            // If this game ticker is a dummy or the round is already being started, do nothing!
            if (DummyTicker || _startingRound)
                return;

            _startingRound = true;

            DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
            Logger.InfoS("ticker", "Starting round!");

            SendServerMessage(Loc.GetString("game-ticker-start-round"));

            LoadMaps();

            StartGamePresetRules();

            RoundLengthMetric.Set(0);

            var playerIds = _playersInLobby.Keys.Select(player => player.UserId.UserId).ToArray();
            // TODO FIXME AAAAAAAAAAAAAAAAAAAH THIS IS BROKEN
            // Task.Run as a terrible dirty workaround to avoid synchronization context deadlock from .Result here.
            // This whole setup logic should be made asynchronous so we can properly wait on the DB AAAAAAAAAAAAAH
            RoundId = Task.Run(async () => await _db.AddNewRound(playerIds)).Result;

            var startingEvent = new RoundStartingEvent();
            RaiseLocalEvent(startingEvent);

            List<IPlayerSession> readyPlayers;
            if (LobbyEnabled)
            {
                readyPlayers = _playersInLobby.Where(p => p.Value == LobbyPlayerStatus.Ready).Select(p => p.Key)
                    .ToList();
            }
            else
            {
                readyPlayers = _playersInLobby.Keys.ToList();
            }

            readyPlayers.RemoveAll(p =>
            {
                if (_roleBanManager.GetRoleBans(p.UserId) != null)
                    return false;
                Logger.ErrorS("RoleBans", $"Role bans for player {p} {p.UserId} have not been loaded yet.");
                return true;
            });

            // Get the profiles for each player for easier lookup.
            var profiles = _prefsManager.GetSelectedProfilesForPlayers(
                    readyPlayers
                        .Select(p => p.UserId).ToList())
                .ToDictionary(p => p.Key, p => (HumanoidCharacterProfile) p.Value);

            foreach (var readyPlayer in readyPlayers)
            {
                if (!profiles.ContainsKey(readyPlayer.UserId))
                {
                    profiles.Add(readyPlayer.UserId, HumanoidCharacterProfile.Random());
                }
            }

            var origReadyPlayers = readyPlayers.ToArray();

            if (!StartPreset(origReadyPlayers, force))
                return;

            // MapInitialize *before* spawning players, our codebase is too shit to do it afterwards...
            _mapManager.DoMapInitialize(DefaultMap);

            SpawnPlayers(readyPlayers, origReadyPlayers, profiles, force);

            _roundStartDateTime = DateTime.UtcNow;
            RunLevel = GameRunLevel.InRound;

            _roundStartTimeSpan = _gameTiming.RealTime;
            SendStatusToAll();
            ReqWindowAttentionAll();
            UpdateLateJoinStatus();
            UpdateJobsAvailable();

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _roundStartFailCount++;

                if (RoundStartFailShutdownCount > 0 && _roundStartFailCount >= RoundStartFailShutdownCount)
                {
                    Logger.FatalS("ticker",
                        $"Failed to start a round {_roundStartFailCount} time(s) in a row... Shutting down!");
                    _runtimeLog.LogException(e, nameof(GameTicker));
                    _baseServer.Shutdown("Restarting server");
                    return;
                }

                Logger.WarningS("ticker", $"Exception caught while trying to start the round! Restarting round...");
                _runtimeLog.LogException(e, nameof(GameTicker));
                _startingRound = false;
                RestartRound();
                return;
            }

            // Round started successfully! Reset counter...
            _roundStartFailCount = 0;
#endif
            _startingRound = false;
        }

        private void RefreshLateJoinAllowed()
        {
            var refresh = new RefreshLateJoinAllowedEvent();
            RaiseLocalEvent(refresh);
            DisallowLateJoin = refresh.DisallowLateJoin;
        }

        public void EndRound(string text = "")
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            Logger.InfoS("ticker", "Ending round!");

            RunLevel = GameRunLevel.PostRound;

            //Tell every client the round has ended.
            var gamemodeTitle = _preset != null ? Loc.GetString(_preset.ModeTitle) : string.Empty;

            // Let things add text here.
            var textEv = new RoundEndTextAppendEvent();
            RaiseLocalEvent(textEv);

            var roundEndText = $"{text}\n{textEv.Text}";

            //Get the timespan of the round.
            var roundDuration = RoundDuration();

            //Generate a list of basic player info to display in the end round summary.
            var listOfPlayerInfo = new List<RoundEndMessageEvent.RoundEndPlayerInfo>();
            // Grab the great big book of all the Minds, we'll need them for this.
            var allMinds = Get<MindTrackerSystem>().AllMinds;
            foreach (var mind in allMinds)
            {
                if (mind != null)
                {
                    // Some basics assuming things fail
                    var userId = mind.OriginalOwnerUserId;
                    var playerOOCName = userId.ToString();
                    var connected = false;
                    var observer = mind.AllRoles.Any(role => role is ObserverRole);
                    // Continuing
                    if (_playerManager.TryGetSessionById(userId, out var ply))
                    {
                        connected = true;
                    }
                    PlayerData? contentPlayerData = null;
                    if (_playerManager.TryGetPlayerData(userId, out var playerData))
                    {
                        contentPlayerData = playerData.ContentData();
                    }
                    // Finish
                    var antag = mind.AllRoles.Any(role => role.Antagonist);

                    var playerIcName = string.Empty;

                    if (mind.CharacterName != null)
                        playerIcName = mind.CharacterName;
                    else if (mind.CurrentEntity != null)
                        playerIcName = EntityManager.GetComponent<MetaDataComponent>(mind.CurrentEntity.Value).EntityName;

                    var playerEndRoundInfo = new RoundEndMessageEvent.RoundEndPlayerInfo()
                    {
                        // Note that contentPlayerData?.Name sticks around after the player is disconnected.
                        // This is as opposed to ply?.Name which doesn't.
                        PlayerOOCName = contentPlayerData?.Name ?? "(IMPOSSIBLE: REGISTERED MIND WITH NO OWNER)",
                        // Character name takes precedence over current entity name
                        PlayerICName = playerIcName,
                        Role = antag
                            ? mind.AllRoles.First(role => role.Antagonist).Name
                            : mind.AllRoles.FirstOrDefault()?.Name ?? Loc.GetString("game-ticker-unknown-role"),
                        Antag = antag,
                        Observer = observer,
                        Connected = connected
                    };
                    listOfPlayerInfo.Add(playerEndRoundInfo);
                }
            }
            // This ordering mechanism isn't great (no ordering of minds) but functions
            var listOfPlayerInfoFinal = listOfPlayerInfo.OrderBy(pi => pi.PlayerOOCName).ToArray();
            _playersInGame.Clear();
            RaiseNetworkEvent(new RoundEndMessageEvent(gamemodeTitle, roundEndText, roundDuration, listOfPlayerInfoFinal.Length, listOfPlayerInfoFinal));
        }

        public void RestartRound()
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            if (_updateOnRoundEnd)
            {
                _baseServer.Shutdown(Loc.GetString("game-ticker-shutdown-server-update"));
                return;
            }

            Logger.InfoS("ticker", "Restarting round!");

            SendServerMessage(Loc.GetString("game-ticker-restart-round"));

            RoundNumberMetric.Inc();

            RunLevel = GameRunLevel.PreRoundLobby;
            LobbySong = _robustRandom.Pick(_lobbyMusicCollection.PickFiles).ToString();
            ResettingCleanup();

            if (!LobbyEnabled)
            {
                StartRound();
            }
            else
            {
                if (_playerManager.PlayerCount == 0)
                    _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
                else
                    _roundStartTime = _gameTiming.CurTime + LobbyDuration;

                SendStatusToAll();

                ReqWindowAttentionAll();
            }
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void ResettingCleanup()
        {
            // Move everybody currently in the server to lobby.
            foreach (var player in _playerManager.ServerSessions)
            {
                PlayerJoinLobby(player);
            }

            // Delete the minds of everybody.
            // TODO: Maybe move this into a separate manager?
            foreach (var unCastData in _playerManager.GetAllPlayerData())
            {
                unCastData.ContentData()?.WipeMind();
            }

            // Delete all entities.
            foreach (var entity in EntityManager.GetEntities().ToArray())
            {
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                EntityManager.DeleteEntity(entity);
            }

            _mapManager.Restart();

            _roleBanManager.Restart();

            // Clear up any game rules.
            ClearGameRules();

            _addedGameRules.Clear();

            // Round restart cleanup event, so entity systems can reset.
            var ev = new RoundRestartCleanupEvent();
            RaiseLocalEvent(ev);

            // So clients' entity systems can clean up too...
            RaiseNetworkEvent(ev, Filter.Broadcast());

            _spawnedPositions.Clear();
            _manifest.Clear();
            DisallowLateJoin = false;
        }

        public bool DelayStart(TimeSpan time)
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
            {
                return false;
            }

            _roundStartTime += time;

            RaiseNetworkEvent(new TickerLobbyCountdownEvent(_roundStartTime, Paused));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("game-ticker-delay-start", ("seconds",time.TotalSeconds)));

            return true;
        }

        private void UpdateRoundFlow(float frameTime)
        {
            if (RunLevel == GameRunLevel.InRound)
            {
                RoundLengthMetric.Inc(frameTime);
            }

            if (RunLevel != GameRunLevel.PreRoundLobby || Paused ||
                _roundStartTime > _gameTiming.CurTime ||
                _roundStartCountdownHasNotStartedYetDueToNoPlayers)
            {
                return;
            }

            StartRound();
        }

        public TimeSpan RoundDuration()
        {
            return _gameTiming.RealTime.Subtract(_roundStartTimeSpan);
        }
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound = 1,
        PostRound = 2
    }

    public sealed class GameRunLevelChangedEvent
    {
        public GameRunLevel Old { get; }
        public GameRunLevel New { get; }

        public GameRunLevelChangedEvent(GameRunLevel old, GameRunLevel @new)
        {
            Old = old;
            New = @new;
        }
    }

    /// <summary>
    ///     Event raised before maps are loaded in pre-round setup.
    ///     Contains a list of game map prototypes to load; modify it if you want to load different maps,
    ///     for example as part of a game rule.
    /// </summary>
    public sealed class LoadingMapsEvent : EntityEventArgs
    {
        public List<GameMapPrototype> Maps;

        public LoadingMapsEvent(List<GameMapPrototype> maps)
        {
            Maps = maps;
        }
    }

    /// <summary>
    ///     Event raised to refresh the late join status.
    ///     If you want to disallow late joins, listen to this and call Disallow.
    /// </summary>
    public sealed class RefreshLateJoinAllowedEvent
    {
        public bool DisallowLateJoin { get; private set; } = false;

        public void Disallow()
        {
            DisallowLateJoin = true;
        }
    }

    /// <summary>
    ///     Attempt event raised on round start.
    ///     This can be listened to by GameRule systems to cancel round start if some condition is not met, like player count.
    /// </summary>
    public sealed class RoundStartAttemptEvent : CancellableEntityEventArgs
    {
        public IPlayerSession[] Players { get; }
        public bool Forced { get; }

        public RoundStartAttemptEvent(IPlayerSession[] players, bool forced)
        {
            Players = players;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised before readied up players are spawned and given jobs by the GameTicker.
    ///     You can use this to spawn people off-station, like in the case of nuke ops or wizard.
    ///     Remove the players you spawned from the PlayerPool and call <see cref="GameTicker.PlayerJoinGame"/> on them.
    /// </summary>
    public sealed class RulePlayerSpawningEvent
    {
        /// <summary>
        ///     Pool of players to be spawned.
        ///     If you want to handle a specific player being spawned, remove it from this list and do what you need.
        /// </summary>
        /// <remarks>If you spawn a player by yourself from this event, don't forget to call <see cref="GameTicker.PlayerJoinGame"/> on them.</remarks>
        public List<IPlayerSession> PlayerPool { get; }
        public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
        public bool Forced { get; }

        public RulePlayerSpawningEvent(List<IPlayerSession> playerPool, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
        {
            PlayerPool = playerPool;
            Profiles = profiles;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised after players were assigned jobs by the GameTicker.
    ///     You can give on-station people special roles by listening to this event.
    /// </summary>
    public sealed class RulePlayerJobsAssignedEvent
    {
        public IPlayerSession[] Players { get; }
        public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
        public bool Forced { get; }

        public RulePlayerJobsAssignedEvent(IPlayerSession[] players, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
        {
            Players = players;
            Profiles = profiles;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised to allow subscribers to add text to the round end summary screen.
    /// </summary>
    public sealed class RoundEndTextAppendEvent
    {
        private bool _doNewLine;

        /// <summary>
        ///     Text to display in the round end summary screen.
        /// </summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>
        ///     Invoke this method to add text to the round end summary screen.
        /// </summary>
        /// <param name="text"></param>
        public void AddLine(string text)
        {
            if (_doNewLine)
                Text += "\n";

            Text += text;
            _doNewLine = true;
        }
    }
}
