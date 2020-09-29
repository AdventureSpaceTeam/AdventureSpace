﻿using System;
using System.Linq;
using Content.Client.Interfaces;
using Content.Client.Interfaces.Chat;
using Content.Client.UserInterface;
using Content.Shared.Input;
using Robust.Client.Console;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using static Content.Shared.SharedGameTicker;

namespace Content.Client.State
{
    public class LobbyState : Robust.Client.State.State
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IClientConsole _console = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IClientGameTicker _clientGameTicker = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;

        [ViewVariables] private CharacterSetupGui _characterSetup;
        [ViewVariables] private LobbyGui _lobby;

        public override void Startup()
        {
            _characterSetup = new CharacterSetupGui(_entityManager, _resourceCache, _preferencesManager,
                _prototypeManager);
            LayoutContainer.SetAnchorPreset(_characterSetup, LayoutContainer.LayoutPreset.Wide);
            _characterSetup.CloseButton.OnPressed += args =>
            {
                _characterSetup.Save();
                _lobby.CharacterPreview.UpdateUI();
                _userInterfaceManager.StateRoot.AddChild(_lobby);
                _userInterfaceManager.StateRoot.RemoveChild(_characterSetup);
            };

            _lobby = new LobbyGui(_entityManager, _resourceCache, _preferencesManager);
            _userInterfaceManager.StateRoot.AddChild(_lobby);

            LayoutContainer.SetAnchorPreset(_lobby, LayoutContainer.LayoutPreset.Wide);

            _chatManager.SetChatBox(_lobby.Chat);
            _lobby.Chat.DefaultChatFormat = "ooc \"{0}\"";

            _lobby.ServerName.Text = _baseClient.GameInfo.ServerName;

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(s => GameScreen.FocusChat(_lobby.Chat)));

            UpdateLobbyUi();

            _lobby.CharacterPreview.CharacterSetupButton.OnPressed += args =>
            {
                SetReady(false);
                _userInterfaceManager.StateRoot.RemoveChild(_lobby);
                _userInterfaceManager.StateRoot.AddChild(_characterSetup);
            };

            _lobby.ObserveButton.OnPressed += args => _console.ProcessCommand("observe");
            _lobby.ReadyButton.OnPressed += args =>
            {
                if (!_clientGameTicker.IsGameStarted)
                {
                    return;
                }

                new LateJoinGui().OpenCentered();
                return;
            };

            _lobby.ReadyButton.OnToggled += args =>
            {
                SetReady(args.Pressed);
            };

            _lobby.LeaveButton.OnPressed += args => _console.ProcessCommand("disconnect");
            _lobby.CreditsButton.OnPressed += args => new CreditsWindow().Open();

            UpdatePlayerList();

            _playerManager.PlayerListUpdated += PlayerManagerOnPlayerListUpdated;
            _clientGameTicker.InfoBlobUpdated += UpdateLobbyUi;
            _clientGameTicker.LobbyStatusUpdated += LobbyStatusUpdated;
            _clientGameTicker.LobbyReadyUpdated += LobbyReadyUpdated;
            _clientGameTicker.LobbyLateJoinStatusUpdated += LobbyLateJoinStatusUpdated;
        }

        public override void Shutdown()
        {
            _playerManager.PlayerListUpdated -= PlayerManagerOnPlayerListUpdated;
            _clientGameTicker.InfoBlobUpdated -= UpdateLobbyUi;
            _clientGameTicker.LobbyStatusUpdated -= LobbyStatusUpdated;
            _clientGameTicker.LobbyReadyUpdated -= LobbyReadyUpdated;
            _clientGameTicker.LobbyLateJoinStatusUpdated -= LobbyLateJoinStatusUpdated;

            _clientGameTicker.Status.Clear();

            _lobby.Dispose();
            _characterSetup.Dispose();
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            if (_clientGameTicker.IsGameStarted)
            {
                _lobby.StartTime.Text = "";
                return;
            }

            string text;

            if (_clientGameTicker.Paused)
            {
                text = Loc.GetString("Paused");
            }
            else
            {
                var difference = _clientGameTicker.StartTime - DateTime.UtcNow;
                if (difference.Ticks < 0)
                {
                    if (difference.TotalSeconds < -5)
                    {
                        text = Loc.GetString("Right Now?");
                    }
                    else
                    {
                        text = Loc.GetString("Right Now");
                    }
                }
                else
                {
                    text = $"{(int) Math.Floor(difference.TotalMinutes)}:{difference.Seconds:D2}";
                }
            }

            _lobby.StartTime.Text = Loc.GetString("Round Starts In: {0}", text);
        }

        private void PlayerManagerOnPlayerListUpdated(object sender, EventArgs e)
        {
            // Remove disconnected sessions from the Ready Dict
            foreach (var p in _clientGameTicker.Status)
            {
                if (!_playerManager.SessionsDict.TryGetValue(p.Key, out _))
                {
                    // This is a shitty fix. Observers can rejoin because they are already in the game.
                    // So we don't delete them, but keep them if they decide to rejoin
                    if (p.Value != PlayerStatus.Observer)
                        _clientGameTicker.Status.Remove(p.Key);
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
            _lobby.ReadyButton.Disabled = _clientGameTicker.DisallowedLateJoin;
        }

        private void UpdateLobbyUi()
        {
            if (_lobby == null)
            {
                return;
            }

            if (_clientGameTicker.IsGameStarted)
            {
                _lobby.ReadyButton.Text = Loc.GetString("Join");
                _lobby.ReadyButton.ToggleMode = false;
                _lobby.ReadyButton.Pressed = false;
            }
            else
            {
                _lobby.StartTime.Text = "";
                _lobby.ReadyButton.Text = Loc.GetString("Ready Up");
                _lobby.ReadyButton.ToggleMode = true;
                _lobby.ReadyButton.Disabled = false;
                _lobby.ReadyButton.Pressed = _clientGameTicker.AreWeReady;
            }

            _lobby.ServerInfo.SetInfoBlob(_clientGameTicker.ServerInfoBlob);
        }

        private void UpdatePlayerList()
        {
            _lobby.OnlinePlayerList.Clear();

            foreach (var session in _playerManager.Sessions.OrderBy(s => s.Name))
            {


                var readyState = "";
                // Don't show ready state if we're ingame
                if (!_clientGameTicker.IsGameStarted)
                {
                    var status = PlayerStatus.NotReady;
                    if (session.UserId == _playerManager.LocalPlayer.UserId)
                        status = _clientGameTicker.AreWeReady ? PlayerStatus.Ready : PlayerStatus.NotReady;
                    else
                        _clientGameTicker.Status.TryGetValue(session.UserId, out status);

                    readyState = status switch
                    {
                        PlayerStatus.NotReady => Loc.GetString("Not Ready"),
                        PlayerStatus.Ready => Loc.GetString("Ready"),
                        PlayerStatus.Observer => Loc.GetString("Observer"),
                        _ => "",
                    };
                }
                _lobby.OnlinePlayerList.AddItem(session.Name, readyState);
            }
        }

        private void SetReady(bool newReady)
        {
            if (_clientGameTicker.IsGameStarted)
            {
                return;
            }

            _console.ProcessCommand($"toggleready {newReady}");
            UpdatePlayerList();
        }
    }
}
