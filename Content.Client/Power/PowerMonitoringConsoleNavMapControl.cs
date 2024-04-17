using Content.Client.Pinpointer.UI;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.Power;

public sealed partial class PowerMonitoringConsoleNavMapControl : NavMapControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    // Cable indexing
    // 0: CableType.HighVoltage
    // 1: CableType.MediumVoltage
    // 2: CableType.Apc

    private readonly Color[] _powerCableColors = { Color.OrangeRed, Color.Yellow, Color.LimeGreen };
    private readonly Vector2[] _powerCableOffsets = { new Vector2(-0.2f, -0.2f), Vector2.Zero, new Vector2(0.2f, 0.2f) };
    private Dictionary<Color, Color> _sRGBLookUp = new Dictionary<Color, Color>();

    public PowerMonitoringCableNetworksComponent? PowerMonitoringCableNetworks;
    public List<PowerMonitoringConsoleLineGroup> HiddenLineGroups = new();
    public List<PowerMonitoringConsoleLine> PowerCableNetwork = new();
    public List<PowerMonitoringConsoleLine> FocusCableNetwork = new();

    private MapGridComponent? _grid;

    public PowerMonitoringConsoleNavMapControl() : base()
    {
        // Set colors
        TileColor = new Color(30, 57, 67);
        WallColor = new Color(102, 164, 217);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

        PostWallDrawingAction += DrawAllCableNetworks;
    }

    protected override void UpdateNavMap()
    {
        base.UpdateNavMap();

        if (Owner == null)
            return;

        if (!_entManager.TryGetComponent<PowerMonitoringCableNetworksComponent>(Owner, out var cableNetworks))
            return;

        PowerCableNetwork = GetDecodedPowerCableChunks(cableNetworks.AllChunks);
        FocusCableNetwork = GetDecodedPowerCableChunks(cableNetworks.FocusChunks);
    }

    public void DrawAllCableNetworks(DrawingHandleScreen handle)
    {
        if (!_entManager.TryGetComponent(MapUid, out _grid))
            return;

        // Draw full cable network
        if (PowerCableNetwork != null && PowerCableNetwork.Count > 0)
        {
            var modulator = (FocusCableNetwork != null && FocusCableNetwork.Count > 0) ? Color.DimGray : Color.White;
            DrawCableNetwork(handle, PowerCableNetwork, modulator);
        }

        // Draw focus network
        if (FocusCableNetwork != null && FocusCableNetwork.Count > 0)
            DrawCableNetwork(handle, FocusCableNetwork, Color.White);
    }

    public void DrawCableNetwork(DrawingHandleScreen handle, List<PowerMonitoringConsoleLine> fullCableNetwork, Color modulator)
    {
        if (!_entManager.TryGetComponent(MapUid, out _grid))
            return;

        var offset = GetOffset();
        offset = offset with { Y = -offset.Y };

        if (WorldRange / WorldMaxRange > 0.5f)
        {
            var cableNetworks = new ValueList<Vector2>[3];

            foreach (var line in fullCableNetwork)
            {
                if (HiddenLineGroups.Contains(line.Group))
                    continue;

                var cableOffset = _powerCableOffsets[(int) line.Group];
                var start = ScalePosition(line.Origin + cableOffset - offset);
                var end = ScalePosition(line.Terminus + cableOffset - offset);

                cableNetworks[(int) line.Group].Add(start);
                cableNetworks[(int) line.Group].Add(end);
            }

            for (int cableNetworkIdx = 0; cableNetworkIdx < cableNetworks.Length; cableNetworkIdx++)
            {
                var cableNetwork = cableNetworks[cableNetworkIdx];

                if (cableNetwork.Count > 0)
                {
                    var color = _powerCableColors[cableNetworkIdx] * modulator;

                    if (!_sRGBLookUp.TryGetValue(color, out var sRGB))
                    {
                        sRGB = Color.ToSrgb(color);
                        _sRGBLookUp[color] = sRGB;
                    }

                    handle.DrawPrimitives(DrawPrimitiveTopology.LineList, cableNetwork.Span, sRGB);
                }
            }
        }

        else
        {
            var cableVertexUVs = new ValueList<Vector2>[3];

            foreach (var line in fullCableNetwork)
            {
                if (HiddenLineGroups.Contains(line.Group))
                    continue;

                var cableOffset = _powerCableOffsets[(int) line.Group];

                var leftTop = ScalePosition(new Vector2
                    (Math.Min(line.Origin.X, line.Terminus.X) - 0.1f,
                    Math.Min(line.Origin.Y, line.Terminus.Y) - 0.1f)
                    + cableOffset - offset);

                var rightTop = ScalePosition(new Vector2
                    (Math.Max(line.Origin.X, line.Terminus.X) + 0.1f,
                    Math.Min(line.Origin.Y, line.Terminus.Y) - 0.1f)
                    + cableOffset - offset);

                var leftBottom = ScalePosition(new Vector2
                    (Math.Min(line.Origin.X, line.Terminus.X) - 0.1f,
                    Math.Max(line.Origin.Y, line.Terminus.Y) + 0.1f)
                    + cableOffset - offset);

                var rightBottom = ScalePosition(new Vector2
                    (Math.Max(line.Origin.X, line.Terminus.X) + 0.1f,
                    Math.Max(line.Origin.Y, line.Terminus.Y) + 0.1f)
                    + cableOffset - offset);

                cableVertexUVs[(int) line.Group].Add(leftBottom);
                cableVertexUVs[(int) line.Group].Add(leftTop);
                cableVertexUVs[(int) line.Group].Add(rightBottom);
                cableVertexUVs[(int) line.Group].Add(leftTop);
                cableVertexUVs[(int) line.Group].Add(rightBottom);
                cableVertexUVs[(int) line.Group].Add(rightTop);
            }

            for (int cableNetworkIdx = 0; cableNetworkIdx < cableVertexUVs.Length; cableNetworkIdx++)
            {
                var cableVertexUV = cableVertexUVs[cableNetworkIdx];

                if (cableVertexUV.Count > 0)
                {
                    var color = _powerCableColors[cableNetworkIdx] * modulator;

                    if (!_sRGBLookUp.TryGetValue(color, out var sRGB))
                    {
                        sRGB = Color.ToSrgb(color);
                        _sRGBLookUp[color] = sRGB;
                    }

                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, cableVertexUV.Span, sRGB);
                }
            }
        }
    }

    public List<PowerMonitoringConsoleLine> GetDecodedPowerCableChunks(Dictionary<Vector2i, PowerCableChunk>? chunks)
    {
        var decodedOutput = new List<PowerMonitoringConsoleLine>();

        if (!_entManager.TryGetComponent(MapUid, out _grid))
            return decodedOutput;

        if (chunks == null)
            return decodedOutput;

        // We'll use the following dictionaries to combine collinear power cable lines
        HorizLinesLookup.Clear();
        HorizLinesLookupReversed.Clear();
        VertLinesLookup.Clear();
        VertLinesLookupReversed.Clear();

        foreach ((var chunkOrigin, var chunk) in chunks)
        {
            for (int cableIdx = 0; cableIdx < chunk.PowerCableData.Length; cableIdx++)
            {
                var chunkMask = chunk.PowerCableData[cableIdx];

                for (var chunkIdx = 0; chunkIdx < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; chunkIdx++)
                {
                    var value = (int) Math.Pow(2, chunkIdx);
                    var mask = chunkMask & value;

                    if (mask == 0x0)
                        continue;

                    var relativeTile = SharedNavMapSystem.GetTile(mask);
                    var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * _grid.TileSize;
                    tile = tile with { Y = -tile.Y };

                    PowerCableChunk neighborChunk;
                    bool neighbor;

                    // Note: we only check the north and east neighbors

                    // East
                    if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out neighborChunk) &&
                                    (neighborChunk.PowerCableData[cableIdx] & SharedNavMapSystem.GetFlag(new Vector2i(0, relativeTile.Y))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(1, 0));
                        neighbor = (chunkMask & flag) != 0x0;
                    }

                    if (neighbor)
                    {
                        // Add points
                        AddOrUpdateNavMapLine(tile, tile + new Vector2i(_grid.TileSize, 0), HorizLinesLookup, HorizLinesLookupReversed, cableIdx);
                    }

                    // North
                    if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out neighborChunk) &&
                                    (neighborChunk.PowerCableData[cableIdx] & SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, 0))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, 1));
                        neighbor = (chunkMask & flag) != 0x0;
                    }

                    if (neighbor)
                    {
                        // Add points
                        AddOrUpdateNavMapLine(tile + new Vector2i(0, -_grid.TileSize), tile, VertLinesLookup, VertLinesLookupReversed, cableIdx);
                    }
                }

            }
        }

        var gridOffset = new Vector2(_grid.TileSize * 0.5f, -_grid.TileSize * 0.5f);

        foreach (var (origin, terminal) in HorizLinesLookup)
            decodedOutput.Add(new PowerMonitoringConsoleLine(origin.Item2 + gridOffset, terminal.Item2 + gridOffset, (PowerMonitoringConsoleLineGroup) origin.Item1));

        foreach (var (origin, terminal) in VertLinesLookup)
            decodedOutput.Add(new PowerMonitoringConsoleLine(origin.Item2 + gridOffset, terminal.Item2 + gridOffset, (PowerMonitoringConsoleLineGroup) origin.Item1));

        return decodedOutput;
    }
}

public struct PowerMonitoringConsoleLine
{
    public readonly Vector2 Origin;
    public readonly Vector2 Terminus;
    public readonly PowerMonitoringConsoleLineGroup Group;

    public PowerMonitoringConsoleLine(Vector2 origin, Vector2 terminus, PowerMonitoringConsoleLineGroup group)
    {
        Origin = origin;
        Terminus = terminus;
        Group = group;
    }
}

public enum PowerMonitoringConsoleLineGroup : byte
{
    HighVoltage,
    MediumVoltage,
    Apc,
}
