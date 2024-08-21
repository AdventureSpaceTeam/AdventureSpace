using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Events;

[Serializable, NetSerializable]
public sealed partial class SurgeryRemoveToolOrganDoAfter : DoAfterEvent
{
    public OrganSlot Slot;

    public SurgeryRemoveToolOrganDoAfter(OrganSlot slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}
