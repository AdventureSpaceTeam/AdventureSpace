using Content.Client.Computer;
using Content.Client.UserInterface;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class ShuttleConsoleWindow : FancyWindow,
    IComputerWindow<ShuttleConsoleBoundInterfaceState>
{
    private readonly IEntityManager _entManager;

    /// <summary>
    /// EntityUid of the open console.
    /// </summary>
    private EntityUid? _entity;

    /// <summary>
    /// Currently selected dock button for camera.
    /// </summary>
    private BaseButton? _selectedDock;

    /// <summary>
    /// Stored by grid entityid then by states
    /// </summary>
    private Dictionary<EntityUid, List<DockingInterfaceState>> _docks = new();

    public Action<ShuttleMode>? ShuttleModePressed;
    public Action<EntityUid>? UndockPressed;
    public Action<EntityUid>? StartAutodockPressed;
    public Action<EntityUid>? StopAutodockPressed;

    public ShuttleConsoleWindow()
    {
        RobustXamlLoader.Load(this);
        _entManager = IoCManager.Resolve<IEntityManager>();

        OnRadarRangeChange(RadarScreen.RadarRange);
        RadarScreen.OnRadarRangeChanged += OnRadarRangeChange;

        IFFToggle.OnToggled += OnIFFTogglePressed;
        IFFToggle.Pressed = RadarScreen.ShowIFF;

        DockToggle.OnToggled += OnDockTogglePressed;
        DockToggle.Pressed = RadarScreen.ShowDocks;

        ShuttleModeDisplay.OnToggled += OnShuttleModePressed;

        UndockButton.OnPressed += OnUndockPressed;
    }

    private void OnRadarRangeChange(float value)
    {
        RadarRange.Text = $"{value:0}";
    }

    private void OnShuttleModePressed(BaseButton.ButtonEventArgs obj)
    {
        ShuttleModePressed?.Invoke(obj.Button.Pressed ? ShuttleMode.Strafing : ShuttleMode.Cruise);
    }

    private void OnIFFTogglePressed(BaseButton.ButtonEventArgs args)
    {
        RadarScreen.ShowIFF ^= true;
        args.Button.Pressed = RadarScreen.ShowIFF;
    }

    private void OnDockTogglePressed(BaseButton.ButtonEventArgs args)
    {
        RadarScreen.ShowDocks ^= true;
        args.Button.Pressed = RadarScreen.ShowDocks;
    }

    private void OnUndockPressed(BaseButton.ButtonEventArgs args)
    {
        if (DockingScreen.ViewedDock == null) return;
        UndockPressed?.Invoke(DockingScreen.ViewedDock.Value);
    }

    public void UpdateState(ShuttleConsoleBoundInterfaceState scc)
    {
        _entity = scc.Entity;
        UpdateDocks(scc.Docks);
        RadarScreen.UpdateState(scc);
        MaxRadarRange.Text = $"{scc.MaxRange:0}";
        ShuttleModeDisplay.Pressed = scc.Mode == ShuttleMode.Strafing;
    }

    #region Docking

    private void UpdateDocks(List<DockingInterfaceState> docks)
    {
        // TODO: We should check for changes so any existing highlighted doesn't delete.
        // We also need to make up some pseudonumber as well for these.
        _docks.Clear();

        foreach (var dock in docks)
        {
            var grid = _docks.GetOrNew(dock.Coordinates.EntityId);
            grid.Add(dock);
        }

        DockPorts.DisposeAllChildren();
        DockingScreen.Docks = _docks;

        if (!_entManager.TryGetComponent<TransformComponent>(_entity, out var xform)
            || !xform.GridUid.HasValue)
        {
            // TODO: Show Placeholder
            return;
        }

        if (_docks.TryGetValue(xform.GridUid.Value, out var gridDocks))
        {
            var index = 1;

            foreach (var state in gridDocks)
            {
                var ent = state.Entity;
                var pressed = ent == DockingScreen.ViewedDock;
                string suffix;

                if (state.Connected)
                {
                    suffix = Loc.GetString("shuttle-console-docked", ("index", index));
                }
                else
                {
                    suffix = $"{index}";
                }

                var button = new Button()
                {
                    Text = Loc.GetString("shuttle-console-dock-button", ("suffix", suffix)),
                    ToggleMode = true,
                    Pressed = pressed,
                };

                button.OnMouseEntered += args => OnDockMouseEntered(args, ent);
                button.OnMouseExited += args => OnDockMouseExited(args, ent);
                button.OnToggled += args => OnDockToggled(args, ent);
                DockPorts.AddChild(button);
                index++;
            }
        }
    }

    private void OnDockMouseEntered(GUIMouseHoverEventArgs obj, EntityUid uid)
    {
        RadarScreen.HighlightedDock = uid;
    }

    private void OnDockMouseExited(GUIMouseHoverEventArgs obj, EntityUid uid)
    {
        RadarScreen.HighlightedDock = null;
    }

    /// <summary>
    /// Shows a docking camera instead of radar screen.
    /// </summary>
    private void OnDockToggled(BaseButton.ButtonEventArgs obj, EntityUid ent)
    {
        if (_selectedDock != null)
        {
            _selectedDock.Pressed = false;
            _selectedDock = null;
        }

        if (!obj.Button.Pressed)
        {
            if (DockingScreen.ViewedDock != null)
            {
                StopAutodockPressed?.Invoke(DockingScreen.ViewedDock.Value);
                DockingScreen.ViewedDock = null;
            }

            UndockButton.Disabled = true;
            DockingScreen.Visible = false;
            RadarScreen.Visible = true;
        }
        else
        {
            // DebugTools.Assert(DockingScreen.ViewedDock == null);
            _entManager.TryGetComponent<TransformComponent>(_entity, out var xform);

            UndockButton.Disabled = false;
            RadarScreen.Visible = false;
            DockingScreen.Visible = true;
            DockingScreen.ViewedDock = ent;
            StartAutodockPressed?.Invoke(ent);
            DockingScreen.GridEntity = xform?.GridUid;
            _selectedDock = obj.Button;
        }
    }

    public override void Close()
    {
        base.Close();
        if (DockingScreen.ViewedDock != null)
        {
            StopAutodockPressed?.Invoke(DockingScreen.ViewedDock.Value);
        }
    }

    #endregion

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (!_entManager.TryGetComponent<TransformComponent>(_entity, out var entXform) ||
            !_entManager.TryGetComponent<PhysicsComponent>(entXform.GridUid, out var gridBody) ||
            !_entManager.TryGetComponent<TransformComponent>(entXform.GridUid, out var gridXform))
        {
            return;
        }

        var (_, worldRot, worldMatrix) = gridXform.GetWorldPositionRotationMatrix();
        var worldPos = worldMatrix.Transform(gridBody.LocalCenter);

        // Get the positive reduced angle.
        var displayRot = -worldRot.Reduced();

        GridPosition.Text = $"{worldPos.X:0.0}, {worldPos.Y:0.0}";
        GridOrientation.Text = $"{displayRot.Degrees:0.0}";

        var gridVelocity = gridBody.LinearVelocity;
        gridVelocity = displayRot.RotateVec(gridVelocity);
        // Get linear velocity relative to the console entity
        GridLinearVelocity.Text = $"{gridVelocity.X:0.0}, {gridVelocity.Y:0.0}";
        GridAngularVelocity.Text = $"{-gridBody.AngularVelocity:0.0}";
    }
}
