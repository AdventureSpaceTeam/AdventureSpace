using System;
using System.Linq;
using Content.Client.Chat.Managers;
using Content.Client.EscapeMenu.UI;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby.UI;
using Content.Client.Preferences;
using Content.Client.Preferences.UI;
using Content.Client.Viewport;
using Content.Client.Voting;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Lobby
{
    public class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;

        [ViewVariables] private CharacterSetupGui _characterSetup = default!;
        [ViewVariables] private LobbyGui _lobby = default!;

        public override void Startup()
        {
            var gameTicker = EntitySystem.Get<ClientGameTicker>();
            _characterSetup = new CharacterSetupGui(_entityManager, _resourceCache, _preferencesManager,
                _prototypeManager);
            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);

            _characterSetup.CloseButton.OnPressed += _ =>
            {
                _userInterfaceManager.StateRoot.AddChild(_lobby);
                _userInterfaceManager.StateRoot.RemoveChild(_characterSetup);
            };

            _characterSetup.SaveButton.OnPressed += _ =>
            {
                _characterSetup.Save();
                _lobby?.CharacterPreview.UpdateUI();
            };

            _lobby = new LobbyGui(_entityManager, _preferencesManager);
            _userInterfaceManager.StateRoot.AddChild(_lobby);

            LayoutContainer.SetAnchorPreset(_lobby, LayoutContainer.LayoutPreset.Wide);

            _chatManager.SetChatBox(_lobby.Chat);
            _voteManager.SetPopupContainer(_lobby.VoteContainer);

            _lobby.Chat.DefaultChatFormat = "ooc \"{0}\"";

            _lobby.ServerName.Text = _baseClient.GameInfo?.ServerName;

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChat(_lobby.Chat)));

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusOOC,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(_lobby.Chat, ChatChannel.OOC)));

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusAdminChat,
                InputCmdHandler.FromDelegate(_ => GameScreen.FocusChannel(_lobby.Chat, ChatChannel.AdminChat)));

            _inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelForward,
                InputCmdHandler.FromDelegate(_ => _lobby.Chat.CycleChatChannel(true)));

            _inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelBackward,
                InputCmdHandler.FromDelegate(_ => _lobby.Chat.CycleChatChannel(false)));

            UpdateLobbyUi();

            _lobby.CharacterPreview.CharacterSetupButton.OnPressed += _ =>
            {
                SetReady(false);
                _userInterfaceManager.StateRoot.RemoveChild(_lobby);
                _userInterfaceManager.StateRoot.AddChild(_characterSetup);
            };

            _lobby.ObserveButton.OnPressed += _ => _consoleHost.ExecuteCommand("observe");
            _lobby.ReadyButton.OnPressed += _ =>
            {
                if (!gameTicker.IsGameStarted)
                {
                    return;
                }

                new LateJoinGui().OpenCentered();
            };

            _lobby.ReadyButton.OnToggled += args =>
            {
                SetReady(args.Pressed);
            };

            _lobby.LeaveButton.OnPressed += _ => _consoleHost.ExecuteCommand("disconnect");
            _lobby.OptionsButton.OnPressed += _ => new OptionsMenu().Open();

            UpdatePlayerList();

            _playerManager.PlayerListUpdated += PlayerManagerOnPlayerListUpdated;
            gameTicker.InfoBlobUpdated += UpdateLobbyUi;
            gameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            gameTicker.LobbyReadyUpdated += LobbyReadyUpdated;
            gameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;
        }

        public override void Shutdown()
        {
            _playerManager.PlayerListUpdated -= PlayerManagerOnPlayerListUpdated;
            _lobby.Dispose();
            _characterSetup.Dispose();
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            var gameTicker = EntitySystem.Get<ClientGameTicker>();
            if (gameTicker.IsGameStarted)
            {
                _lobby.StartTime.Text = string.Empty;
                return;
            }

            string text;

            if (gameTicker.Paused)
            {
                text = Loc.GetString("lobby-state-paused");
            }
            else
            {
                var difference = gameTicker.StartTime - _gameTiming.CurTime;
                var seconds = difference.TotalSeconds;
                if (seconds < 0)
                {
                    text = Loc.GetString(seconds < -5 ? "lobby-state-right-now-question" : "lobby-state-right-now-confirmation");
                }
                else
                {
                    text = $"{(int) Math.Floor(difference.TotalMinutes / 60)}:{difference.Seconds:D2}";
                }
            }

            _lobby.StartTime.Text = Loc.GetString("lobby-state-round-start-countdown-text", ("timeLeft", text));
        }

        private void PlayerManagerOnPlayerListUpdated(object? sender, EventArgs e)
        {
            var gameTicker = EntitySystem.Get<ClientGameTicker>();
            // Remove disconnected sessions from the Ready Dict
            foreach (var p in gameTicker.Status)
            {
                if (!_playerManager.SessionsDict.TryGetValue(p.Key, out _))
                {
                    // This is a shitty fix. Observers can rejoin because they are already in the game.
                    // So we don't delete them, but keep them if they decide to rejoin
                    if (p.Value != LobbyPlayerStatus.Observer)
                        gameTicker.Status.Remove(p.Key);
                }
            }

            UpdatePlayerList();
        }

        private void LobbyReadyUpdated() => UpdatePlayerList();

        private void LobbyStatusUpdated()
        {
            UpdatePlayerList();
            UpdateLobbyUi();
        }

        private void LobbyLateJoinStatusUpdated()
        {
            _lobby.ReadyButton.Disabled = EntitySystem.Get<ClientGameTicker>().DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_lobby == null)
            {
                return;
            }

            var gameTicker = EntitySystem.Get<ClientGameTicker>();

            if (gameTicker.IsGameStarted)
            {
                _lobby.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-join-state");
                _lobby.ReadyButton.ToggleMode = false;
                _lobby.ReadyButton.Pressed = false;
            }
            else
            {
                _lobby.StartTime.Text = string.Empty;
                _lobby.ReadyButton.Text = Loc.GetString("lobby-state-ready-button-ready-up-state");
                _lobby.ReadyButton.ToggleMode = true;
                _lobby.ReadyButton.Disabled = false;
                _lobby.ReadyButton.Pressed = gameTicker.AreWeReady;
            }

            if (gameTicker.ServerInfoBlob != null)
            {
                _lobby.ServerInfo.SetInfoBlob(gameTicker.ServerInfoBlob);
            }
        }

        private void UpdatePlayerList()
        {
            _lobby.OnlinePlayerList.Clear();
            var gameTicker = EntitySystem.Get<ClientGameTicker>();

            foreach (var session in _playerManager.Sessions.OrderBy(s => s.Name))
            {
                var readyState = string.Empty;
                // Don't show ready state if we're ingame
                if (!gameTicker.IsGameStarted)
                {
                    LobbyPlayerStatus status;
                    if (session.UserId == _playerManager.LocalPlayer?.UserId)
                        status = gameTicker.AreWeReady ? LobbyPlayerStatus.Ready : LobbyPlayerStatus.NotReady;
                    else
                        gameTicker.Status.TryGetValue(session.UserId, out status);

                    readyState = status switch
                    {
                        LobbyPlayerStatus.NotReady => Loc.GetString("lobby-state-player-status-not-ready"),
                        LobbyPlayerStatus.Ready => Loc.GetString("lobby-state-player-status-ready"),
                        LobbyPlayerStatus.Observer => Loc.GetString("lobby-state-player-status-observer"),
                        _ => string.Empty,
                    };
                }

                _lobby.OnlinePlayerList.AddItem(session.Name, readyState);
            }
        }

        private void SetReady(bool newReady)
        {
            if (EntitySystem.Get<ClientGameTicker>().IsGameStarted)
            {
                return;
            }

            _consoleHost.ExecuteCommand($"toggleready {newReady}");
            UpdatePlayerList();
        }
    }
}
