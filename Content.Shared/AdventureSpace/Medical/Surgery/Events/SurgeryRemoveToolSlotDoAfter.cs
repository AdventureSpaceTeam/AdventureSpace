using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Events;

[Serializable, NetSerializable]
public sealed partial class SurgeryRemoveToolSlotDoAfter : DoAfterEvent
{
    public BodyPartSlot Slot;

    public SurgeryRemoveToolSlotDoAfter(BodyPartSlot slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}
