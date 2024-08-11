using Content.Server._DTS;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server._c4llv07e;

[PublicAPI]
public sealed class MapSystem : EntitySystem
{
    [ViewVariables(VVAccess.ReadOnly)]
    private string? _previousMap = string.Empty;

    public override void Initialize()
    {
        SubscribeLocalEvent<MainStationMapSelected>(OnMainMapSelected);
    }

    private void OnMainMapSelected(MainStationMapSelected ev)
    {
        _previousMap = ev.MainStation.MapName;
    }

    public string? GetPreviousMap()
    {
        return _previousMap;
    }
}
