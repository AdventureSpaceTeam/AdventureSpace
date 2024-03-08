using System.Buffers;
using System.Numerics;
using Content.Client.Shuttles.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.UI.MapObjects;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class ShuttleMapControl : BaseShuttleControl
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputs = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly ShuttleSystem _shuttles;
    private readonly SharedTransformSystem _xformSystem;

    protected override bool Draggable => true;

    public bool ShowBeacons = true;
    public MapId ViewingMap = MapId.Nullspace;

    private EntityUid? _shuttleEntity;

    private readonly Font _font;

    private readonly EntityQuery<PhysicsComponent> _physicsQuery;

    /// <summary>
    /// Toggles FTL mode on. This shows a pre-vis for FTLing a grid.
    /// </summary>
    public bool FtlMode;

    private Angle _ftlAngle;

    /// <summary>
    /// Are we currently in FTL.
    /// </summary>
    public bool InFtl;

    /// <summary>
    /// Raised when a request to FTL to a particular spot is raised.
    /// </summary>
    public event Action<MapCoordinates, Angle>? RequestFTL;

    public event Action<NetEntity, Angle>? RequestBeaconFTL;

    /// <summary>
    /// Set every draw to determine the beacons that are clickable for mouse events
    /// </summary>
    private List<IMapObject> _beacons = new();

    // Per frame data to avoid re-allocating
    private readonly List<IMapObject> _mapObjects = new();
    private readonly Dictionary<Color, List<Vector2>> _verts = new();
    private readonly Dictionary<Color, List<Vector2>> _edges = new();
    private readonly Dictionary<Color, List<(Vector2, string)>> _strings = new();
    private readonly List<ShuttleExclusionObject> _viewportExclusions = new();

    public ShuttleMapControl() : base(256f, 512f, 512f)
    {
        RobustXamlLoader.Load(this);
        _shuttles = EntManager.System<ShuttleSystem>();
        _xformSystem = EntManager.System<SharedTransformSystem>();
        var cache = IoCManager.Resolve<IResourceCache>();

        _physicsQuery = EntManager.GetEntityQuery<PhysicsComponent>();

        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    public void SetMap(MapId mapId, Vector2 offset, bool recentering = false)
    {
        ViewingMap = mapId;
        TargetOffset = offset;
        Recentering = recentering;
    }

    public void SetShuttle(EntityUid? entity)
    {
        _shuttleEntity = entity;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        // No move for you.
        if (FtlMode)
            return;

        base.MouseMove(args);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (FtlMode && ViewingMap != MapId.Nullspace)
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                var mapUid = _mapManager.GetMapEntityId(ViewingMap);

                var beaconsOnly = EntManager.TryGetComponent(mapUid, out FTLDestinationComponent? destComp) &&
                                  destComp.BeaconsOnly;

                var mapTransform = Matrix3.CreateInverseTransform(Offset, Angle.Zero);

                if (beaconsOnly && TryGetBeacon(_beacons, mapTransform, args.RelativePosition, PixelRect, out var foundBeacon, out _))
                {
                    RequestBeaconFTL?.Invoke(foundBeacon.Entity, _ftlAngle);
                }
                else
                {
                    // We'll send the "adjusted" position and server will adjust it back when relevant.
                    var mapCoords = new MapCoordinates(InverseMapPosition(args.RelativePosition), ViewingMap);
                    RequestFTL?.Invoke(mapCoords, _ftlAngle);
                }
            }
        }

        base.KeyBindUp(args);
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        // Scroll handles FTL rotation if you're in FTL mode.
        if (FtlMode)
        {
            _ftlAngle += Angle.FromDegrees(15f) * args.Delta.Y;
            _ftlAngle = _ftlAngle.Reduced();
            return;
        }

        base.MouseWheel(args);
    }

    private void DrawParallax(DrawingHandleScreen handle)
    {
        if (!EntManager.TryGetComponent(_shuttleEntity, out TransformComponent? shuttleXform) || shuttleXform.MapUid == null)
            return;

        // TODO: Figure out how the fuck to make this common between the 3 slightly different parallax methods and move to parallaxsystem.
        // Draw background texture
        var tex = _shuttles.GetTexture(shuttleXform.MapUid.Value);

        // Size of the texture in world units.
        var size = tex.Size * MinimapScale * 1f;

        var position = ScalePosition(new Vector2(-Offset.X, Offset.Y));
        var slowness = 1f;

        // The "home" position is the effective origin of this layer.
        // Parallax shifting is relative to the home, and shifts away from the home and towards the Eye centre.
        // The effects of this are such that a slowness of 1 anchors the layer to the centre of the screen, while a slowness of 0 anchors the layer to the world.
        // (For values 0.0 to 1.0 this is in effect a lerp, but it's deliberately unclamped.)
        // The ParallaxAnchor adapts the parallax for station positioning and possibly map-specific tweaks.
        var home = Vector2.Zero;
        var scrolled = Vector2.Zero;

        // Origin - start with the parallax shift itself.
        var originBL = (position - home) * slowness + scrolled;

        // Place at the home.
        originBL += home;

        // Centre the image.
        originBL -= size / 2;

        // Remove offset so we can floor.
        var botLeft = new Vector2(0f, 0f);
        var topRight = botLeft + Size;

        var flooredBL = botLeft - originBL;

        // Floor to background size.
        flooredBL = (flooredBL / size).Floored() * size;

        // Re-offset.
        flooredBL += originBL;

        for (var x = flooredBL.X; x < topRight.X; x += size.X)
        {
            for (var y = flooredBL.Y; y < topRight.Y; y += size.Y)
            {
                handle.DrawTextureRect(tex, new UIBox2(x, y, x + size.X, y + size.Y));
            }
        }
    }

    /// <summary>
    /// Gets the map objects that intersect the viewport.
    /// </summary>
    /// <param name="mapObjects"></param>
    /// <returns></returns>
    private List<IMapObject> GetViewportMapObjects(Matrix3 matty, List<IMapObject> mapObjects)
    {
        var results = new List<IMapObject>();
        var viewBox = SizeBox.Scale(1.2f);

        foreach (var mapObj in mapObjects)
        {
            // If it's a grid-map skip it.
            if (mapObj is GridMapObject gridObj && EntManager.HasComponent<MapComponent>(gridObj.Entity))
                continue;

            var mapCoords = _shuttles.GetMapCoordinates(mapObj);

            var relativePos = matty.Transform(mapCoords.Position);
            relativePos = relativePos with { Y = -relativePos.Y };
            var uiPosition = ScalePosition(relativePos);

            if (!viewBox.Contains(uiPosition.Floored()))
                continue;

            results.Add(mapObj);
        }

        return results;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (ViewingMap == MapId.Nullspace)
            return;

        var mapObjects = _mapObjects;
        DrawRecenter();

        if (InFtl || mapObjects.Count == 0)
        {
            DrawBacking(handle);
            DrawNoSignal(handle);
            return;
        }

        DrawParallax(handle);

        var viewedMapUid = _mapManager.GetMapEntityId(ViewingMap);
        var matty = Matrix3.CreateInverseTransform(Offset, Angle.Zero);
        var realTime = _timing.RealTime;
        var viewBox = new Box2(Offset - WorldRangeVector, Offset + WorldRangeVector);
        var viewportObjects = GetViewportMapObjects(matty, mapObjects);
        _viewportExclusions.Clear();

        // Draw our FTL range + no FTL zones
        // Do it up here because we want this layered below most things.
        if (FtlMode)
        {
            if (EntManager.TryGetComponent<TransformComponent>(_shuttleEntity, out var shuttleXform))
            {
                var gridUid = _shuttleEntity.Value;
                var gridPhysics = _physicsQuery.GetComponent(gridUid);
                var (gridPos, gridRot) = _xformSystem.GetWorldPositionRotation(shuttleXform);
                gridPos = Maps.GetGridPosition((gridUid, gridPhysics), gridPos, gridRot);

                var gridRelativePos = matty.Transform(gridPos);
                gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
                var gridUiPos = ScalePosition(gridRelativePos);

                var range = _shuttles.GetFTLRange(gridUid);
                range *= MinimapScale;
                handle.DrawCircle(gridUiPos, range, Color.Gold, filled: false);
            }
        }

        var exclusionColor = Color.Red;

        // Exclusions need a bumped range so we check all the ones on the map.
        foreach (var mapObj in mapObjects)
        {
            if (mapObj is not ShuttleExclusionObject exclusion)
                continue;

            // Check if it even intersects the viewport.
            var coords = EntManager.GetCoordinates(exclusion.Coordinates);
            var mapCoords = _xformSystem.ToMapCoordinates(coords);
            var enlargedBounds = viewBox.Enlarged(exclusion.Range);

            if (mapCoords.MapId != ViewingMap ||
                !enlargedBounds.Contains(mapCoords.Position))
            {
                continue;
            }

            var adjustedPos = matty.Transform(mapCoords.Position);
            var localPos = ScalePosition(adjustedPos with { Y = -adjustedPos.Y});
            handle.DrawCircle(localPos, exclusion.Range * MinimapScale, exclusionColor.WithAlpha(0.05f));
            handle.DrawCircle(localPos, exclusion.Range * MinimapScale, exclusionColor, filled: false);

            _viewportExclusions.Add(exclusion);
        }

        _verts.Clear();
        _edges.Clear();
        _strings.Clear();

        // Add beacons if relevant.
        var beaconsOnly = _shuttles.IsBeaconMap(viewedMapUid);
        var controlLocalBounds = PixelRect;
        _beacons.Clear();

        if (ShowBeacons)
        {
            var beaconColor = Color.AliceBlue;

            foreach (var (beaconName, coords, mapO) in GetBeacons(viewportObjects, matty, controlLocalBounds))
            {
                var localPos = matty.Transform(coords.Position);
                localPos = localPos with { Y = -localPos.Y };
                var beaconUiPos = ScalePosition(localPos);
                var mapObject = GetMapObject(localPos, Angle.Zero, scale: 0.75f, scalePosition: true);

                var existingVerts = _verts.GetOrNew(beaconColor);
                var existingEdges = _edges.GetOrNew(beaconColor);

                AddMapObject(existingEdges, existingVerts, mapObject);
                _beacons.Add(mapO);

                var existingStrings = _strings.GetOrNew(beaconColor);
                existingStrings.Add((beaconUiPos, beaconName));
            }
        }

        foreach (var mapObj in viewportObjects)
        {
            if (mapObj is not GridMapObject gridObj || !EntManager.TryGetComponent(gridObj.Entity, out MapGridComponent? mapGrid))
                continue;

            Entity<MapGridComponent> grid = (gridObj.Entity, mapGrid);
            IFFComponent? iffComp = null;

            // Rudimentary IFF for now, if IFF hiding on then we don't show on the map at all
            if (grid.Owner != _shuttleEntity &&
                EntManager.TryGetComponent(grid, out iffComp) &&
                (iffComp.Flags & (IFFFlags.Hide | IFFFlags.HideLabel)) != 0x0)
            {
                continue;
            }

            var gridColor = _shuttles.GetIFFColor(grid, self: _shuttleEntity == grid.Owner, component: iffComp);

            var existingVerts = _verts.GetOrNew(gridColor);
            var existingEdges = _edges.GetOrNew(gridColor);

            var gridPhysics = _physicsQuery.GetComponent(grid.Owner);
            var (gridPos, gridRot) = _xformSystem.GetWorldPositionRotation(grid.Owner);
            gridPos = Maps.GetGridPosition((grid, gridPhysics), gridPos, gridRot);

            var gridRelativePos = matty.Transform(gridPos);
            gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
            var gridUiPos = ScalePosition(gridRelativePos);

            var mapObject = GetMapObject(gridRelativePos, Angle.Zero, scalePosition: true);
            AddMapObject(existingEdges, existingVerts, mapObject);

            // Text
            // Force drawing it at this point.
            var iffText = _shuttles.GetIFFLabel(grid, self: true, component: iffComp);

            if (string.IsNullOrEmpty(iffText))
                continue;

            var existingStrings = _strings.GetOrNew(gridColor);
            existingStrings.Add((gridUiPos, iffText));
        }

        // Batch the colors whoopie
        // really only affects forks with lots of grids.
        foreach (var (color, sendVerts) in _verts)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, sendVerts, color.WithAlpha(0.05f));
        }

        foreach (var (color, sendEdges) in _edges)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.LineList, sendEdges, color);
        }

        foreach (var (color, sendStrings) in _strings)
        {
            var adjustedColor = Color.FromSrgb(color);

            foreach (var (gridUiPos, iffText) in sendStrings)
            {
                var textWidth = handle.GetDimensions(_font, iffText, UIScale);
                handle.DrawString(_font, gridUiPos + textWidth with { X = -textWidth.X / 2f }, iffText, adjustedColor);
            }
        }

        var mousePos = _inputs.MouseScreenPosition;
        var mouseLocalPos = GetLocalPosition(mousePos);

        // Draw dotted line from our own shuttle entity to mouse.
        if (FtlMode)
        {
            if (mousePos.Window != WindowId.Invalid)
            {
                // If mouse inbounds then draw it.
                if (_shuttleEntity != null && controlLocalBounds.Contains(mouseLocalPos.Floored()) &&
                    EntManager.TryGetComponent(_shuttleEntity, out TransformComponent? shuttleXform) &&
                    shuttleXform.MapID != MapId.Nullspace)
                {
                    // If it's a beacon only map then snap the mouse to a nearby spot.
                    ShuttleBeaconObject foundBeacon = default;

                    // Check for beacons around mouse and snap to that.
                    if (beaconsOnly && TryGetBeacon(viewportObjects, matty, mouseLocalPos, controlLocalBounds, out foundBeacon, out var foundLocalPos))
                    {
                        mouseLocalPos = foundLocalPos;
                    }

                    var grid = EntManager.GetComponent<MapGridComponent>(_shuttleEntity.Value);

                    var (gridPos, gridRot) = _xformSystem.GetWorldPositionRotation(shuttleXform);
                    gridPos = Maps.GetGridPosition(_shuttleEntity.Value, gridPos, gridRot);

                    // do NOT apply LocalCenter operation here because it will be adjusted in FTLFree.
                    var mouseMapPos = InverseMapPosition(mouseLocalPos);

                    var ftlFree = (!beaconsOnly || foundBeacon != default) &&
                                  _shuttles.FTLFree(_shuttleEntity.Value, new EntityCoordinates(viewedMapUid, mouseMapPos), _ftlAngle, _viewportExclusions);

                    var color = ftlFree ? Color.LimeGreen : Color.Magenta;

                    var gridRelativePos = matty.Transform(gridPos);
                    gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
                    var gridUiPos = ScalePosition(gridRelativePos);

                    // Draw FTL buffer around the mouse.
                    var ourFTLBuffer = _shuttles.GetFTLBufferRange(_shuttleEntity.Value, grid);
                    ourFTLBuffer *= MinimapScale;
                    handle.DrawCircle(mouseLocalPos, ourFTLBuffer, Color.Magenta.WithAlpha(0.01f));
                    handle.DrawCircle(mouseLocalPos, ourFTLBuffer, Color.Magenta, filled: false);

                    // Draw line from our shuttle to target
                    // Might need to clip the line if it's too far? But my brain wasn't working so F.
                    handle.DrawDottedLine(gridUiPos, mouseLocalPos, color, (float) realTime.TotalSeconds * 30f);

                    // Draw shuttle pre-vis
                    var mouseVerts = GetMapObject(mouseLocalPos, _ftlAngle, scale: MinimapScale);

                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, mouseVerts.Span, color.WithAlpha(0.05f));
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineLoop, mouseVerts.Span, color);

                    // Draw a notch indicating direction.
                    var ftlLength = GetMapObjectRadius() + 16f;
                    var ftlEnd = mouseLocalPos + _ftlAngle.RotateVec(new Vector2(0f, -ftlLength));

                    handle.DrawLine(mouseLocalPos, ftlEnd, color);
                }
            }
        }

        // Draw the coordinates
        var mapOffset = MidPointVector;

        if (mousePos.Window != WindowId.Invalid &&
            controlLocalBounds.Contains(mouseLocalPos.Floored()))
        {
            mapOffset = mouseLocalPos;
        }

        mapOffset = InverseMapPosition(mapOffset);
        var coordsText = $"{mapOffset.X:0.0}, {mapOffset.Y:0.0}";
        DrawData(handle, coordsText);
    }

    private void AddMapObject(List<Vector2> edges, List<Vector2> verts, ValueList<Vector2> mapObject)
    {
        var bottom = mapObject[0];
        var right = mapObject[1];
        var top = mapObject[2];
        var left = mapObject[3];

        // Diamond interior
        verts.Add(bottom);
        verts.Add(right);
        verts.Add(top);

        verts.Add(bottom);
        verts.Add(top);
        verts.Add(left);

        // Diamond edges
        edges.Add(bottom);
        edges.Add(right);
        edges.Add(right);
        edges.Add(top);
        edges.Add(top);
        edges.Add(left);
        edges.Add(left);
        edges.Add(bottom);
    }

    /// <summary>
    /// Returns the beacons that intersect the viewport.
    /// </summary>
    private IEnumerable<(string Beacon, MapCoordinates Coordinates, IMapObject MapObject)> GetBeacons(List<IMapObject> mapObjs, Matrix3 mapTransform, UIBox2i area)
    {
        foreach (var mapO in mapObjs)
        {
            if (mapO is not ShuttleBeaconObject beacon)
                continue;

            var beaconCoords = EntManager.GetCoordinates(beacon.Coordinates).ToMap(EntManager, _xformSystem);
            var position = mapTransform.Transform(beaconCoords.Position);
            var localPos = ScalePosition(position with {Y = -position.Y});

            // If beacon not on screen then ignore it.
            if (!area.Contains(localPos.Floored()))
                continue;

            yield return (beacon.Name, beaconCoords, mapO);
        }
    }

    private float GetMapObjectRadius(float scale = 1f) => WorldRange / 40f * scale;

    private ValueList<Vector2> GetMapObject(Vector2 localPos, Angle angle, float scale = 1f, bool scalePosition = false)
    {
        // Constant size diamonds
        var diamondRadius = GetMapObjectRadius();

        var mapObj = new ValueList<Vector2>(4)
        {
            localPos + angle.RotateVec(new Vector2(0f, -2f * diamondRadius)) * scale,
            localPos + angle.RotateVec(new Vector2(diamondRadius, 0f)) * scale,
            localPos + angle.RotateVec(new Vector2(0f, 2f * diamondRadius)) * scale,
            localPos + angle.RotateVec(new Vector2(-diamondRadius, 0f)) * scale,
        };

        if (scalePosition)
        {
            for (var i = 0; i < mapObj.Count; i++)
            {
                mapObj[i] = ScalePosition(mapObj[i]);
            }
        }

        return mapObj;
    }

    private bool TryGetBeacon(IEnumerable<IMapObject> mapObjects, Matrix3 mapTransform, Vector2 mousePos, UIBox2i area, out ShuttleBeaconObject foundBeacon, out Vector2 foundLocalPos)
    {
        // In pixels
        const float BeaconSnapRange = 32f;
        float nearestValue = float.MaxValue;
        foundLocalPos = Vector2.Zero;
        foundBeacon = default;

        foreach (var mapObj in mapObjects)
        {
            if (mapObj is not ShuttleBeaconObject beaconObj)
                continue;

            var beaconCoords = _xformSystem.ToMapCoordinates(EntManager.GetCoordinates(beaconObj.Coordinates));

            if (beaconCoords.MapId != ViewingMap)
                continue;

            // Invalid beacon?
            if (!_shuttles.CanFTLBeacon(beaconObj.Coordinates))
                continue;

            var position = mapTransform.Transform(beaconCoords.Position);
            var localPos = ScalePosition(position with {Y = -position.Y});

            // If beacon not on screen then ignore it.
            if (!area.Contains(localPos.Floored()))
                continue;

            var distance = (localPos - mousePos).Length();

            if (distance > BeaconSnapRange ||
                distance > nearestValue)
            {
                continue;
            }

            foundLocalPos = localPos;
            nearestValue = distance;
            foundBeacon = beaconObj;
        }

        return foundBeacon != default;
    }

    /// <summary>
    /// Sets the map objects for the next draw.
    /// </summary>
    public void SetMapObjects(Dictionary<MapId, List<IMapObject>> mapObjects)
    {
        _mapObjects.Clear();

        if (mapObjects.TryGetValue(ViewingMap, out var obbies))
        {
            _mapObjects.AddRange(obbies);
        }
    }
}
