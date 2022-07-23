using Content.Client.Computer;
using Content.Client.UserInterface;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class ShuttleConsoleWindow : FancyWindow,
    IComputerWindow<ShuttleConsoleBoundInterfaceState>
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;

    private EntityUid? _shuttleUid;

    /// <summary>
    /// Currently selected dock button for camera.
    /// </summary>
    private BaseButton? _selectedDock;

    /// <summary>
    /// Stored by grid entityid then by states
    /// </summary>
    private readonly Dictionary<EntityUid, List<DockingInterfaceState>> _docks = new();

    private readonly Dictionary<BaseButton, EntityUid> _destinations = new();

    /// <summary>
    /// Next FTL state change.
    /// </summary>
    public TimeSpan FTLTime;

    public Action<EntityUid>? UndockPressed;
    public Action<EntityUid>? StartAutodockPressed;
    public Action<EntityUid>? StopAutodockPressed;
    public Action<EntityUid>? DestinationPressed;

    public ShuttleConsoleWindow()
    {
        RobustXamlLoader.Load(this);
        _entManager = IoCManager.Resolve<IEntityManager>();
        _timing = IoCManager.Resolve<IGameTiming>();

        OnRadarRangeChange(RadarScreen.RadarRange);
        RadarScreen.OnRadarRangeChanged += OnRadarRangeChange;

        IFFToggle.OnToggled += OnIFFTogglePressed;
        IFFToggle.Pressed = RadarScreen.ShowIFF;

        DockToggle.OnToggled += OnDockTogglePressed;
        DockToggle.Pressed = RadarScreen.ShowDocks;

        UndockButton.OnPressed += OnUndockPressed;
    }

    private void OnRadarRangeChange(float value)
    {
        RadarRange.Text = $"{value:0}";
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

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _shuttleUid = coordinates?.EntityId;
        RadarScreen.SetMatrix(coordinates, angle);
    }

    public void UpdateState(ShuttleConsoleBoundInterfaceState scc)
    {
        UpdateDocks(scc.Docks);
        UpdateFTL(scc.Destinations, scc.FTLState, scc.FTLTime);
        RadarScreen.UpdateState(scc);
        MaxRadarRange.Text = $"{scc.MaxRange:0}";
    }

    private void UpdateFTL(List<(EntityUid Entity, string Destination, bool Enabled)> destinations, FTLState state, TimeSpan time)
    {
        HyperspaceDestinations.DisposeAllChildren();
        _destinations.Clear();

        if (destinations.Count == 0)
        {
            HyperspaceDestinations.AddChild(new Label()
            {
                Text = Loc.GetString("shuttle-console-hyperspace-none"),
                HorizontalAlignment = HAlignment.Center,
            });
        }
        else
        {
            destinations.Sort((x, y) => string.Compare(x.Destination, y.Destination, StringComparison.Ordinal));

            foreach (var destination in destinations)
            {
                var button = new Button()
                {
                    Disabled = !destination.Enabled,
                    Text = destination.Destination,
                };

                _destinations[button] = destination.Entity;
                button.OnPressed += OnHyperspacePressed;
                HyperspaceDestinations.AddChild(button);
            }
        }

        string stateText;

        switch (state)
        {
            case Shared.Shuttles.Systems.FTLState.Available:
                stateText = Loc.GetString("shuttle-console-ftl-available");
                break;
            case Shared.Shuttles.Systems.FTLState.Starting:
                stateText = Loc.GetString("shuttle-console-ftl-starting");
                break;
            case Shared.Shuttles.Systems.FTLState.Travelling:
                stateText = Loc.GetString("shuttle-console-ftl-travelling");
                break;
            case Shared.Shuttles.Systems.FTLState.Cooldown:
                stateText = Loc.GetString("shuttle-console-ftl-cooldown");
                break;
            case Shared.Shuttles.Systems.FTLState.Arriving:
                stateText = Loc.GetString("shuttle-console-ftl-arriving");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        FTLState.Text = stateText;
        // Add a buffer due to lag or whatever
        time += TimeSpan.FromSeconds(0.3);
        FTLTime = time;
        FTLTimer.Text = GetFTLText();
    }

    private string GetFTLText()
    {
        return $"{Math.Max(0, (FTLTime - _timing.CurTime).TotalSeconds):0.0}";
    }

    private void OnHyperspacePressed(BaseButton.ButtonEventArgs obj)
    {
        var ent = _destinations[obj.Button];
        DestinationPressed?.Invoke(ent);
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

        if (_shuttleUid != null && _docks.TryGetValue(_shuttleUid.Value, out var gridDocks))
        {
            var index = 1;

            foreach (var state in gridDocks)
            {
                var pressed = state.Entity == DockingScreen.ViewedDock;

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
                    Margin = new Thickness(0f, 1f),
                };

                if (pressed)
                {
                    _selectedDock = button;
                }

                button.OnMouseEntered += args => OnDockMouseEntered(args, state);
                button.OnMouseExited += args => OnDockMouseExited(args, state);
                button.OnToggled += args => OnDockToggled(args, state);
                DockPorts.AddChild(button);
                index++;
            }
        }
    }

    private void OnDockMouseEntered(GUIMouseHoverEventArgs obj, DockingInterfaceState state)
    {
        RadarScreen.HighlightedDock = state.Entity;
    }

    private void OnDockMouseExited(GUIMouseHoverEventArgs obj, DockingInterfaceState state)
    {
        RadarScreen.HighlightedDock = null;
    }

    /// <summary>
    /// Shows a docking camera instead of radar screen.
    /// </summary>
    private void OnDockToggled(BaseButton.ButtonEventArgs obj, DockingInterfaceState state)
    {
        var ent = state.Entity;

        if (_selectedDock != null)
        {
            // If it got untoggled via other means then we'll stop viewing the old dock.
            if (DockingScreen.ViewedDock != null && DockingScreen.ViewedDock != state.Entity)
            {
                StopAutodockPressed?.Invoke(DockingScreen.ViewedDock.Value);
            }

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
            if (_shuttleUid != null)
            {
                DockingScreen.Coordinates = state.Coordinates;
                DockingScreen.Angle = state.Angle;
            }
            else
            {
                DockingScreen.Coordinates = null;
                DockingScreen.Angle = null;
            }

            UndockButton.Disabled = false;
            RadarScreen.Visible = false;
            DockingScreen.Visible = true;
            DockingScreen.ViewedDock = ent;
            StartAutodockPressed?.Invoke(ent);
            DockingScreen.GridEntity = _shuttleUid;
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

        if (!_entManager.TryGetComponent<PhysicsComponent>(_shuttleUid, out var gridBody) ||
            !_entManager.TryGetComponent<TransformComponent>(_shuttleUid, out var gridXform))
        {
            return;
        }

        if (_entManager.TryGetComponent<MetaDataComponent>(_shuttleUid, out var metadata) && metadata.EntityPaused)
        {
            FTLTime += _timing.FrameTime;
        }

        FTLTimer.Text = GetFTLText();

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
