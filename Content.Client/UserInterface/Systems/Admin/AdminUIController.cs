﻿using Content.Client.Administration.Managers;
using Content.Client.Administration.UI;
using Content.Client.Administration.UI.Tabs.PlayerTab;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Admin;

[UsedImplicitly]
public sealed class AdminUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IClientConGroupController _conGroups = default!;
    [Dependency] private readonly IClientConsoleHost _conHost = default!;
    [Dependency] private readonly IInputManager _input = default!;

    [UISystemDependency] private readonly VerbSystem _verbs = default!;

    private AdminMenuWindow? _window;
    private MenuButton? _adminButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);
        _window = UIManager.CreateWindow<AdminMenuWindow>();
        _adminButton = UIManager.GetActiveUIWidget<MenuBar.Widgets.GameTopMenuBar>().AdminButton;
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.Center);
        _window.PlayerTabControl.OnEntryPressed += PlayerTabEntryPressed;
        _window.OnOpen += () => _adminButton.Pressed = true;
        _window.OnClose += () => _adminButton.Pressed = false;

        _admin.AdminStatusUpdated += AdminStatusUpdated;

        _adminButton.OnPressed += AdminButtonPressed;

        _input.SetInputCommand(ContentKeyFunctions.OpenAdminMenu,
            InputCmdHandler.FromDelegate(_ => Toggle()));

        AdminStatusUpdated();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }

        _admin.AdminStatusUpdated -= AdminStatusUpdated;

        if (_adminButton != null)
        {
            _adminButton.Pressed = false;
            _adminButton.OnPressed -= AdminButtonPressed;
            _adminButton = null;
        }

        CommandBinds.Unregister<AdminUIController>();
    }

    private void AdminStatusUpdated()
    {
        _adminButton!.Visible = _conGroups.CanAdminMenu();
    }

    private void AdminButtonPressed(ButtonEventArgs args)
    {
        Toggle();
    }

    private void Toggle()
    {
        if (_window is {IsOpen: true})
        {
            _window.Close();
        }
        else if (_conGroups.CanAdminMenu())
        {
            _window?.Open();
        }
    }

    private void PlayerTabEntryPressed(ButtonEventArgs args)
    {
        if (args.Button is not PlayerTabEntry button
            || button.PlayerUid == null)
            return;

        var uid = button.PlayerUid.Value;
        var function = args.Event.Function;

        if (function == EngineKeyFunctions.UIClick)
            _conHost.ExecuteCommand($"vv {uid}");
        else if (function == EngineKeyFunctions.UseSecondary)
            _verbs.VerbMenu.OpenVerbMenu(uid, true);
        else
            return;

        args.Event.Handle();
    }
}
