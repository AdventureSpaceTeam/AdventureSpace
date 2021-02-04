﻿using System;
using Content.Client.UserInterface;
using Content.Shared.Input;
using Content.Shared.Sandbox;
using Robust.Client.Console;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client.Sandbox
{
    // Layout for the SandboxWindow
    public class SandboxWindow : SS14Window
    {
        public readonly Button RespawnButton;
        public readonly Button SpawnEntitiesButton;
        public readonly Button SpawnTilesButton;
        public readonly Button GiveFullAccessButton;  //A button that just puts a captain's ID in your hands.
        public readonly Button GiveAghostButton;
        public readonly Button ToggleLightButton;
        public readonly Button ToggleFovButton;
        public readonly Button ToggleShadowsButton;
        public readonly Button SuicideButton;
        public readonly Button ToggleSubfloorButton;
        public readonly Button ShowMarkersButton; //Shows spawn points
        public readonly Button ShowBbButton; //Shows bounding boxes
        public readonly Button MachineLinkingButton; // Enables/disables machine linking mode.
        private readonly IGameHud _gameHud;

        public SandboxWindow()
        {
            Resizable = false;
            _gameHud = IoCManager.Resolve<IGameHud>();

            Title = "Sandbox Panel";

            var vBox = new VBoxContainer { SeparationOverride = 4 };
            Contents.AddChild(vBox);

            RespawnButton = new Button { Text = Loc.GetString("Respawn") };
            vBox.AddChild(RespawnButton);

            SpawnEntitiesButton = new Button { Text = Loc.GetString("Spawn Entities") };
            vBox.AddChild(SpawnEntitiesButton);

            SpawnTilesButton = new Button { Text = Loc.GetString("Spawn Tiles") };
            vBox.AddChild(SpawnTilesButton);

            GiveFullAccessButton = new Button { Text = Loc.GetString("Grant Full Access") };
            vBox.AddChild(GiveFullAccessButton);

            GiveAghostButton = new Button { Text = Loc.GetString("Ghost") };
            vBox.AddChild(GiveAghostButton);

            ToggleLightButton = new Button { Text = Loc.GetString("Toggle Lights"), ToggleMode = true };
            vBox.AddChild(ToggleLightButton);

            ToggleFovButton = new Button { Text = Loc.GetString("Toggle FOV"), ToggleMode = true };
            vBox.AddChild(ToggleFovButton);

            ToggleShadowsButton = new Button { Text = Loc.GetString("Toggle Shadows"), ToggleMode = true };
            vBox.AddChild(ToggleShadowsButton);

            ToggleSubfloorButton = new Button { Text = Loc.GetString("Toggle Subfloor"), ToggleMode = true };
            vBox.AddChild(ToggleSubfloorButton);

            SuicideButton = new Button { Text = Loc.GetString("Suicide") };
            vBox.AddChild(SuicideButton);

            ShowMarkersButton = new Button { Text = Loc.GetString("Show Spawns"), ToggleMode = true };
            vBox.AddChild(ShowMarkersButton);

            ShowBbButton = new Button { Text = Loc.GetString("Show BB"), ToggleMode = true };
            vBox.AddChild(ShowBbButton);

            MachineLinkingButton = new Button { Text = Loc.GetString("Link machines"), ToggleMode = true };
            vBox.AddChild(MachineLinkingButton);
        }


        protected override void EnteredTree()
        {
            base.EnteredTree();
            _gameHud.SandboxButtonDown = true;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _gameHud.SandboxButtonDown = false;
        }

    }

    internal class SandboxManager : SharedSandboxManager, ISandboxManager
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        public bool SandboxAllowed { get; private set; }

        public event Action<bool> AllowedChanged;

        private SandboxWindow _window;
        private EntitySpawnWindow _spawnWindow;
        private TileSpawnWindow _tilesSpawnWindow;
        private bool _sandboxWindowToggled;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgSandboxStatus>(nameof(MsgSandboxStatus),
                message => SetAllowed(message.SandboxAllowed));

            _netManager.RegisterNetMessage<MsgSandboxGiveAccess>(nameof(MsgSandboxGiveAccess));

            _netManager.RegisterNetMessage<MsgSandboxRespawn>(nameof(MsgSandboxRespawn));

            _netManager.RegisterNetMessage<MsgSandboxGiveAghost>(nameof(MsgSandboxGiveAghost));

            _netManager.RegisterNetMessage<MsgSandboxSuicide>(nameof(MsgSandboxSuicide));

            _gameHud.SandboxButtonToggled = SandboxButtonPressed;

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenEntitySpawnWindow,
                InputCmdHandler.FromDelegate(session => ToggleEntitySpawnWindow()));
            _inputManager.SetInputCommand(ContentKeyFunctions.OpenSandboxWindow,
                InputCmdHandler.FromDelegate(session => ToggleSandboxWindow()));
            _inputManager.SetInputCommand(ContentKeyFunctions.OpenTileSpawnWindow,
                InputCmdHandler.FromDelegate(session => ToggleTilesWindow()));
        }

        private void SandboxButtonPressed(bool newValue)
        {
            _sandboxWindowToggled = newValue;
            UpdateSandboxWindowVisibility();
        }

        private void ToggleSandboxWindow()
        {
            _sandboxWindowToggled = !_sandboxWindowToggled;
            UpdateSandboxWindowVisibility();
        }

        private void UpdateSandboxWindowVisibility()
        {
            if (_sandboxWindowToggled && SandboxAllowed)
                OpenWindow();
            else
                _window?.Close();
        }

        private void SetAllowed(bool newAllowed)
        {
            if (newAllowed == SandboxAllowed)
            {
                return;
            }

            SandboxAllowed = newAllowed;
            _gameHud.SandboxButtonVisible = newAllowed;

            if (!newAllowed)
            {
                // Sandbox permission revoked, close window.
                _window?.Close();
            }

            AllowedChanged?.Invoke(newAllowed);
        }

        private void OpenWindow()
        {
            if (_window != null)
            {
                return;
            }

            _window = new SandboxWindow();

            _window.OnClose += WindowOnOnClose;

            _window.RespawnButton.OnPressed += OnRespawnButtonOnOnPressed;
            _window.SpawnTilesButton.OnPressed += OnSpawnTilesButtonClicked;
            _window.SpawnEntitiesButton.OnPressed += OnSpawnEntitiesButtonClicked;
            _window.GiveFullAccessButton.OnPressed += OnGiveAdminAccessButtonClicked;
            _window.GiveAghostButton.OnPressed += OnGiveAghostButtonClicked;
            _window.ToggleLightButton.OnToggled += OnToggleLightButtonClicked;
            _window.ToggleFovButton.OnToggled += OnToggleFovButtonClicked;
            _window.ToggleShadowsButton.OnToggled += OnToggleShadowsButtonClicked;
            _window.SuicideButton.OnPressed += OnSuicideButtonClicked;
            _window.ToggleSubfloorButton.OnPressed += OnToggleSubfloorButtonClicked;
            _window.ShowMarkersButton.OnPressed += OnShowMarkersButtonClicked;
            _window.ShowBbButton.OnPressed += OnShowBbButtonClicked;
            _window.MachineLinkingButton.OnPressed += OnMachineLinkingButtonClicked;

            _window.OpenCentered();
        }

        private void WindowOnOnClose()
        {
            _window = null;
            _sandboxWindowToggled = false;
        }

        private void OnRespawnButtonOnOnPressed(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxRespawn>());
        }

        private void OnSpawnEntitiesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleEntitySpawnWindow();
        }

        private void OnSpawnTilesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleTilesWindow();
        }

        private void OnToggleLightButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleLight();
        }

        private void OnToggleFovButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleFov();
        }

        private void OnToggleShadowsButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleShadows();
        }

        private void OnToggleSubfloorButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ToggleSubFloor();
        }

        private void OnShowMarkersButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ShowMarkers();
        }

        private void OnShowBbButtonClicked(BaseButton.ButtonEventArgs args)
        {
            ShowBb();
        }
        private void OnMachineLinkingButtonClicked(BaseButton.ButtonEventArgs args)
        {
            LinkMachines();
        }

        private void OnGiveAdminAccessButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxGiveAccess>());
        }

        private void OnGiveAghostButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxGiveAghost>());
        }

        private void OnSuicideButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _netManager.ClientSendMessage(_netManager.CreateNetMessage<MsgSandboxSuicide>());
        }

        private void ToggleEntitySpawnWindow()
        {
            if (_spawnWindow == null)
                _spawnWindow = new EntitySpawnWindow(_placementManager, _prototypeManager, _resourceCache);

            if (_spawnWindow.IsOpen)
            {
                _spawnWindow.Close();
            }
            else
            {
                _spawnWindow = new EntitySpawnWindow(_placementManager, _prototypeManager, _resourceCache);
                _spawnWindow.OpenToLeft();
            }
        }

        private void ToggleTilesWindow()
        {
            if (_tilesSpawnWindow == null)
                _tilesSpawnWindow = new TileSpawnWindow(_tileDefinitionManager, _placementManager, _resourceCache);

            if (_tilesSpawnWindow.IsOpen)
            {
                _tilesSpawnWindow.Close();
            }
            else
            {
                _tilesSpawnWindow = new TileSpawnWindow(_tileDefinitionManager, _placementManager, _resourceCache);
                _tilesSpawnWindow.OpenToLeft();
            }
        }

        private void ToggleLight()
        {
            _consoleHost.ExecuteCommand("togglelight");
        }

        private void ToggleFov()
        {
            _consoleHost.ExecuteCommand("togglefov");
        }

        private void ToggleShadows()
        {
            _consoleHost.ExecuteCommand("toggleshadows");
        }

        private void ToggleSubFloor()
        {
            _consoleHost.ExecuteCommand("showsubfloor");
        }

        private void ShowMarkers()
        {
            _consoleHost.ExecuteCommand("showmarkers");
        }

        private void ShowBb()
        {
            _consoleHost.ExecuteCommand("showbb");
        }

        private void LinkMachines()
        {
            _consoleHost.ExecuteCommand("signallink");
        }
    }
}
