using Content.Client.Computer;
using Content.Client.UserInterface;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class RadarConsoleWindow : FancyWindow,
    IComputerWindow<RadarConsoleBoundInterfaceState>
{
    public RadarConsoleWindow()
    {
        RobustXamlLoader.Load(this);
    }

    public void UpdateState(RadarConsoleBoundInterfaceState scc)
    {
        RadarScreen.UpdateState(scc);
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        RadarScreen.SetMatrix(coordinates, angle);
    }
}
