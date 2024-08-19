using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Vampire;

[Serializable, NetSerializable]
public sealed partial class VampireHypnoseDoAfterEvent : DoAfterEvent
{
    public VampireHypnoseDoAfterEvent()
    {
    }
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
