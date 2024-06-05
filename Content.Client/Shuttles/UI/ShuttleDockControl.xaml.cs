using System.Numerics;
using Content.Client.Shuttles.Systems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class ShuttleDockControl : BaseShuttleControl
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly DockingSystem _dockSystem;
    private readonly SharedShuttleSystem _shuttles;
    private readonly SharedTransformSystem _xformSystem;

    public NetEntity? HighlightedDock;

    public NetEntity? ViewedDock => _viewedState?.Entity;
    private DockingPortState? _viewedState;

    public EntityUid? GridEntity;

    private EntityCoordinates? _coordinates;
    private Angle? _angle;

    public DockingInterfaceState? DockState = null;

    private List<Entity<MapGridComponent>> _grids = new();

    private readonly HashSet<DockingPortState> _drawnDocks = new();
    private readonly Dictionary<DockingPortState, Button> _dockButtons = new();

    /// <summary>
    /// Store buttons for every other dock
    /// </summary>
    private readonly Dictionary<DockingPortState, Control> _dockContainers = new();

    private static readonly TimeSpan DockChangeCooldown = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Rate-limiting for docking changes
    /// </summary>
    private TimeSpan _nextDockChange;

    public event Action<NetEntity>? OnViewDock;
    public event Action<NetEntity, NetEntity>? DockRequest;
    public event Action<NetEntity>? UndockRequest;

    public ShuttleDockControl() : base(2f, 32f, 8f)
    {
        RobustXamlLoader.Load(this);
        _dockSystem = EntManager.System<DockingSystem>();
        _shuttles = EntManager.System<SharedShuttleSystem>();
        _xformSystem = EntManager.System<SharedTransformSystem>();
        MinSize = new Vector2(SizeFull, SizeFull);
    }

    public void SetViewedDock(DockingPortState? dockState)
    {
        _viewedState = dockState;

        if (dockState != null)
        {
            _coordinates = EntManager.GetCoordinates(dockState.Coordinates);
            _angle = dockState.Angle;
            OnViewDock?.Invoke(dockState.Entity);
        }
        else
        {
            _coordinates = null;
            _angle = null;
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        HideDocks();
        _drawnDocks.Clear();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        DrawBacking(handle);

        if (_coordinates == null ||
            _angle == null ||
            DockState == null ||
            !EntManager.TryGetComponent<TransformComponent>(GridEntity, out var gridXform))
        {
            DrawNoSignal(handle);
            return;
        }

        DrawCircles(handle);
        var gridNent = EntManager.GetNetEntity(GridEntity);
        var mapPos = _xformSystem.ToMapCoordinates(_coordinates.Value);
        var ourGridMatrix = _xformSystem.GetWorldMatrix(gridXform.Owner);
        var dockMatrix = Matrix3Helpers.CreateTransform(_coordinates.Value.Position, Angle.Zero);
        var worldFromDock = Matrix3x2.Multiply(dockMatrix, ourGridMatrix);

        Matrix3x2.Invert(worldFromDock, out var offsetMatrix);

        // Draw nearby grids
        var controlBounds = PixelSizeBox;
        _grids.Clear();
        _mapManager.FindGridsIntersecting(gridXform.MapID, new Box2(mapPos.Position - WorldRangeVector, mapPos.Position + WorldRangeVector), ref _grids);

        // offset the dotted-line position to the bounds.
        Vector2? viewedDockPos = _viewedState != null ? MidPointVector : null;

        if (viewedDockPos != null)
        {
            viewedDockPos = viewedDockPos.Value + _angle.Value.RotateVec(new Vector2(0f,-0.6f) * MinimapScale);
        }

        var canDockChange = _timing.CurTime > _nextDockChange;
        var lineOffset = (float) _timing.RealTime.TotalSeconds * 30f;

        foreach (var grid in _grids)
        {
            EntManager.TryGetComponent(grid.Owner, out IFFComponent? iffComp);

            if (grid.Owner != GridEntity && !_shuttles.CanDraw(grid.Owner, iffComp: iffComp))
                continue;

            var gridMatrix = _xformSystem.GetWorldMatrix(grid.Owner);
            var matty = Matrix3x2.Multiply(gridMatrix, offsetMatrix);
            var color = _shuttles.GetIFFColor(grid.Owner, grid.Owner == GridEntity, component: iffComp);

            DrawGrid(handle, matty, grid, color);

            // Draw any docks on that grid
            if (!DockState.Docks.TryGetValue(EntManager.GetNetEntity(grid), out var gridDocks))
                continue;

            foreach (var dock in gridDocks)
            {
                if (ViewedDock == dock.Entity)
                    continue;

                var position = Vector2.Transform(dock.Coordinates.Position, matty);

                var otherDockRotation = Matrix3Helpers.CreateRotation(dock.Angle);
                var scaledPos = ScalePosition(position with {Y = -position.Y});

                if (!controlBounds.Contains(scaledPos.Floored()))
                    continue;

                // Draw the dock's collision
                var collisionBL = Vector2.Transform(dock.Coordinates.Position +
                                                  Vector2.Transform(new Vector2(-0.2f, -0.7f), otherDockRotation), matty);
                var collisionBR = Vector2.Transform(dock.Coordinates.Position +
                                                  Vector2.Transform(new Vector2(0.2f, -0.7f), otherDockRotation), matty);
                var collisionTR = Vector2.Transform(dock.Coordinates.Position +
                                                  Vector2.Transform(new Vector2(0.2f, -0.5f), otherDockRotation), matty);
                var collisionTL = Vector2.Transform(dock.Coordinates.Position +
                                                  Vector2.Transform(new Vector2(-0.2f, -0.5f), otherDockRotation), matty);

                var verts = new[]
                {
                    collisionBL,
                    collisionBR,
                    collisionBR,
                    collisionTR,
                    collisionTR,
                    collisionTL,
                    collisionTL,
                    collisionBL,
                };

                for (var i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    vert.Y = -vert.Y;
                    verts[i] = ScalePosition(vert);
                }

                var collisionCenter = verts[0] + verts[1] + verts[3] + verts[5];

                var otherDockConnection = Color.ToSrgb(Color.Pink);
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, otherDockConnection.WithAlpha(0.2f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, verts, otherDockConnection);

                // Draw the dock itself
                var dockBL = Vector2.Transform(dock.Coordinates.Position + new Vector2(-0.5f, -0.5f), matty);
                var dockBR = Vector2.Transform(dock.Coordinates.Position + new Vector2(0.5f, -0.5f), matty);
                var dockTR = Vector2.Transform(dock.Coordinates.Position + new Vector2(0.5f, 0.5f), matty);
                var dockTL = Vector2.Transform(dock.Coordinates.Position + new Vector2(-0.5f, 0.5f), matty);

                verts = new[]
                {
                    dockBL,
                    dockBR,
                    dockBR,
                    dockTR,
                    dockTR,
                    dockTL,
                    dockTL,
                    dockBL
                };

                for (var i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    vert.Y = -vert.Y;
                    verts[i] = ScalePosition(vert);
                }

                Color otherDockColor;

                if (HighlightedDock == dock.Entity)
                {
                    otherDockColor = Color.ToSrgb(Color.Magenta);
                }
                else
                {
                    otherDockColor = Color.ToSrgb(Color.Purple);
                }

                /*
                 * Can draw in these conditions:
                 * 1. Same grid
                 * 2. It's in range
                 *
                 * We don't want to draw stuff far away that's docked because it will just overlap our buttons
                 */

                var canDraw = grid.Owner == GridEntity;
                _dockButtons.TryGetValue(dock, out var dockButton);

                // Rate limit
                if (dockButton != null && dock.GridDockedWith != null)
                {
                    dockButton.Disabled = !canDockChange;
                }

                // If the dock is in range then also do highlighting
                if (viewedDockPos != null && dock.Coordinates.NetEntity != gridNent)
                {
                    collisionCenter /= 4;
                    var range = viewedDockPos.Value - collisionCenter;

                    if (range.Length() < SharedDockingSystem.DockingHiglightRange * MinimapScale)
                    {
                        if (_viewedState?.GridDockedWith == null)
                        {
                            var coordsOne = EntManager.GetCoordinates(_viewedState!.Coordinates);
                            var coordsTwo = EntManager.GetCoordinates(dock.Coordinates);
                            var mapOne = _xformSystem.ToMapCoordinates(coordsOne);
                            var mapTwo = _xformSystem.ToMapCoordinates(coordsTwo);

                            var rotA = _xformSystem.GetWorldRotation(coordsOne.EntityId) + _viewedState!.Angle;
                            var rotB = _xformSystem.GetWorldRotation(coordsTwo.EntityId) + dock.Angle;

                            var distance = (mapOne.Position - mapTwo.Position).Length();

                            var inAlignment = _dockSystem.InAlignment(mapOne, rotA, mapTwo, rotB);
                            var canDock = distance < SharedDockingSystem.DockRange && inAlignment;

                            if (dockButton != null)
                                dockButton.Disabled = !canDock || !canDockChange;

                            var lineColor = inAlignment ? Color.Lime : Color.Red;
                            handle.DrawDottedLine(viewedDockPos.Value, collisionCenter, lineColor, offset: lineOffset);
                        }

                        canDraw = true;
                    }
                    else
                    {
                        if (dockButton != null)
                            dockButton.Disabled = true;
                    }
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, otherDockColor.WithAlpha(0.2f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, verts, otherDockColor);

                // Position the dock control above it
                var container = _dockContainers[dock];
                container.Visible = canDraw;

                if (canDraw)
                {
                    // Because it's being layed out top-down we have to arrange for first frame.
                    container.Arrange(PixelRect);
                    var containerPos = scaledPos / UIScale - container.DesiredSize / 2 - new Vector2(0f, 0.75f) * MinimapScale;
                    SetPosition(container, containerPos);
                }

                _drawnDocks.Add(dock);
            }
        }

        // Draw the dock's collision
        var invertedPosition = Vector2.Zero;
        invertedPosition.Y = -invertedPosition.Y;
        var rotation = Matrix3Helpers.CreateRotation(-_angle.Value + MathF.PI);
        var ourDockConnection = new UIBox2(
            ScalePosition(Vector2.Transform(new Vector2(-0.2f, -0.7f), rotation)),
            ScalePosition(Vector2.Transform(new Vector2(0.2f, -0.5f), rotation)));

        var ourDock = new UIBox2(
            ScalePosition(Vector2.Transform(new Vector2(-0.5f, 0.5f), rotation)),
            ScalePosition(Vector2.Transform(new Vector2(0.5f, -0.5f), rotation)));

        var dockColor = Color.Magenta;
        var connectionColor = Color.Pink;

        handle.DrawRect(ourDockConnection, connectionColor.WithAlpha(0.2f));
        handle.DrawRect(ourDockConnection, connectionColor, filled: false);

        // Draw the dock itself
        handle.DrawRect(ourDock, dockColor.WithAlpha(0.2f));
        handle.DrawRect(ourDock, dockColor, filled: false);
    }

    private void HideDocks()
    {
        foreach (var (dock, control) in _dockContainers)
        {
            if (_drawnDocks.Contains(dock))
                continue;

            control.Visible = false;
        }
    }

    public void BuildDocks(EntityUid? shuttle)
    {
        var viewedEnt = ViewedDock;
        _viewedState = null;

        foreach (var btn in _dockButtons.Values)
        {
            btn.Dispose();
        }

        foreach (var container in _dockContainers.Values)
        {
            container.Dispose();
        }

        _dockButtons.Clear();
        _dockContainers.Clear();

        if (DockState == null)
            return;

        var gridNent = EntManager.GetNetEntity(GridEntity);

        foreach (var (otherShuttle, docks) in DockState.Docks)
        {
            // If it's our shuttle we add a view button

            foreach (var dock in docks)
            {
                if (dock.Entity == viewedEnt)
                {
                    _viewedState = dock;
                }

                var container = new BoxContainer()
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Margin = new Thickness(3),
                };

                var panel = new PanelContainer()
                {
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    PanelOverride = new StyleBoxFlat(new Color(30, 30, 34, 200)),
                    Children =
                    {
                        container,
                    }
                };

                Button button;

                if (otherShuttle == gridNent)
                {
                    button = new Button()
                    {
                        Text = Loc.GetString("shuttle-console-view"),
                    };

                    button.OnPressed += args =>
                    {
                        SetViewedDock(dock);
                    };
                }
                else
                {
                    if (dock.Connected)
                    {
                        button = new Button()
                        {
                            Text = Loc.GetString("shuttle-console-undock"),
                        };

                        button.OnPressed += args =>
                        {
                            _nextDockChange = _timing.CurTime + DockChangeCooldown;
                            UndockRequest?.Invoke(dock.Entity);
                        };
                    }
                    else
                    {
                        button = new Button()
                        {
                            Text = Loc.GetString("shuttle-console-dock"),
                            Disabled = true,
                        };

                        button.OnPressed += args =>
                        {
                            if (ViewedDock == null)
                                return;

                            _nextDockChange = _timing.CurTime + DockChangeCooldown;
                            DockRequest?.Invoke(ViewedDock.Value, dock.Entity);
                        };
                    }

                    _dockButtons.Add(dock, button);
                }

                container.AddChild(new Label()
                {
                    Text = dock.Name,
                    HorizontalAlignment = HAlignment.Center,
                });

                button.HorizontalAlignment = HAlignment.Center;
                container.AddChild(button);

                AddChild(panel);
                panel.Measure(Vector2Helpers.Infinity);
                _dockContainers[dock] = panel;
            }
        }
    }
}
