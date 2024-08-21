using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.DarkForces.Ratvar.DoAfterEvents;

[Serializable, NetSerializable]
public sealed partial class RatvarSoulVesselDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
