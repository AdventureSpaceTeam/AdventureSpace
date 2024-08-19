using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Roles.StationAI.Events;

[Serializable, NetSerializable]
public sealed partial class StationAIMidasDoAfter : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
