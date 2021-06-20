using Content.Server.Players;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    [UsedImplicitly]
    public partial class GameTicker
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private void InitializePlayer()
        {
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
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
                        session.Data.ContentDataUncast = new PlayerData(session.UserId);

                    // Make the player actually join the game.
                    // timer time must be > tick length
                    Timer.Spawn(0, args.Session.JoinGame);

                    _chatManager.SendAdminAnnouncement(Loc.GetString("player-join-message", ("name", args.Session.Name)));

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTime = _gameTiming.CurTime + LobbyDuration;
                    }

                    break;
                }

                case SessionStatus.InGame:
                {
                    _prefsManager.OnClientConnected(session);

                    var data = session.ContentData();

                    DebugTools.AssertNotNull(data);

                    if (data!.Mind == null)
                    {
                        if (LobbyEnabled)
                        {
                            PlayerJoinLobby(session);
                            return;
                        }


                        SpawnWaitPrefs();
                    }
                    else
                    {
                        if (data.Mind.CurrentEntity == null)
                        {
                            SpawnWaitPrefs();
                        }
                        else
                        {
                            session.AttachToEntity(data.Mind.CurrentEntity);
                            PlayerJoinGame(session);
                        }
                    }

                    break;
                }

                case SessionStatus.Disconnected:
                {
                    if (_playersInLobby.ContainsKey(session)) _playersInLobby.Remove(session);

                    _chatManager.SendAdminAnnouncement(Loc.GetString("player-leave-message", ("name", args.Session.Name)));

                    ServerEmptyUpdateRestartCheck();
                    _prefsManager.OnClientDisconnected(session);
                    break;
                }
            }

            async void SpawnWaitPrefs()
            {
                await _prefsManager.WaitPreferencesLoaded(session);
                SpawnPlayer(session);
            }
        }

        private HumanoidCharacterProfile GetPlayerProfile(IPlayerSession p)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(p.UserId).SelectedCharacter;
        }

        private void PlayerJoinGame(IPlayerSession session)
        {
            _chatManager.DispatchServerMessage(session,
                "Welcome to Space Station 14! If this is your first time checking out the game, be sure to check out the tutorial in the top left!");

            if (_playersInLobby.ContainsKey(session))
                _playersInLobby.Remove(session);

            RaiseNetworkEvent(new TickerJoinGameEvent(), session.ConnectedClient);
        }

        private void PlayerJoinLobby(IPlayerSession session)
        {
            _playersInLobby[session] = LobbyPlayerStatus.NotReady;

            var client = session.ConnectedClient;
            RaiseNetworkEvent(new TickerJoinLobbyEvent(), client);
            RaiseNetworkEvent(GetStatusMsg(session), client);
            RaiseNetworkEvent(GetInfoMsg(), client);
            RaiseNetworkEvent(GetPlayerStatus(), client);
            RaiseNetworkEvent(GetJobsAvailable(), client);
        }

        private void ReqWindowAttentionAll()
        {
            RaiseNetworkEvent(new RequestWindowAttentionEvent());
        }
    }
}
