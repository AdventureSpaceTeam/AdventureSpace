using System;
using Content.Client.Computer;
using Content.Shared.Radar;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Maths;

namespace Content.Client.Radar;

[GenerateTypedNameReferences]
public sealed partial class RadarConsoleWindow : DefaultWindow, IComputerWindow<RadarConsoleBoundInterfaceState>
{
    public RadarConsoleWindow()
    {
        RobustXamlLoader.Load(this);
    }

    public void SetupComputerWindow(ComputerBoundUserInterfaceBase cb)
    {

    }

    public void UpdateState(RadarConsoleBoundInterfaceState scc)
    {
        Radar.UpdateState(scc);
    }
}


public sealed class RadarControl : Control
{
    private const int MinimapRadius = 256;
    private const int MinimapMargin = 4;
    private const float GridLinesDistance = 32f;

    private float _radarRange = 256f;
    private RadarConsoleBoundInterfaceState _lastState = new(256f, Array.Empty<RadarObjectData>());

    private int SizeFull => (int) ((MinimapRadius + MinimapMargin) * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    private float MinimapScale => _radarRange != 0 ? ScaledMinimapRadius / _radarRange : 0f;

    public RadarControl()
    {
        MinSize = (SizeFull, SizeFull);
    }

    public void UpdateState(RadarConsoleBoundInterfaceState ls)
    {
        if (!_radarRange.Equals(ls.Range))
        {
            _radarRange = ls.Range;
        }

        _lastState = ls;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var point = SizeFull / 2;
        var fakeAA = new Color(0.08f, 0.08f, 0.08f);
        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(_radarRange / GridLinesDistance);

        handle.DrawCircle((point, point), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((point, point), ScaledMinimapRadius, Color.Black);

        for (var i = 1; i < gridLinesEquatorial + 1; i++)
        {
            handle.DrawCircle((point, point), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine((point, point) - aExtent, (point, point) + aExtent, gridLines);
        }

        handle.DrawLine((point, point) + new Vector2(8, 8), (point, point) - new Vector2(0, 8), Color.Yellow);
        handle.DrawLine((point, point) + new Vector2(-8, 8), (point, point) - new Vector2(0, 8), Color.Yellow);

        foreach (var obj in _lastState.Objects)
        {
            var minimapPos = obj.Position * MinimapScale;
            var radius = obj.Radius * MinimapScale;

            if (minimapPos.Length + radius > ScaledMinimapRadius)
                continue;

            switch (obj.Shape)
            {
                case RadarObjectShape.CircleFilled:
                case RadarObjectShape.Circle:
                {
                    handle.DrawCircle(minimapPos + point, radius, obj.Color, obj.Shape == RadarObjectShape.CircleFilled);
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

[UsedImplicitly]
public sealed class RadarConsoleBoundUserInterface : ComputerBoundUserInterface<RadarConsoleWindow, RadarConsoleBoundInterfaceState>
{
    public RadarConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}
}
