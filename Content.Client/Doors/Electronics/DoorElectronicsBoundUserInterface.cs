using System.Linq;
using Content.Shared.Access;
using Content.Shared.Doors.Electronics;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Doors.Electronics;

public sealed class DoorElectronicsBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private DoorElectronicsConfigurationMenu? _window;

    public DoorElectronicsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        List<string> accessLevels;


        accessLevels = _prototypeManager
            .EnumeratePrototypes<AccessLevelPrototype>()
            .Where(x => x.Name != null)
            .Select(x => x.ID)
            .ToList();
        accessLevels.Sort();

        _window = new DoorElectronicsConfigurationMenu(this, accessLevels, _prototypeManager);
        _window.OnClose += Close;
        _window.OpenCentered();

        SendMessage(new DoorElectronicsRefreshUiMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (DoorElectronicsConfigurationState) state;

        _window?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }

    public void UpdateConfiguration(List<string> newAccessList)
    {
        SendMessage(new DoorElectronicsUpdateConfigurationMessage(newAccessList));
    }
}
