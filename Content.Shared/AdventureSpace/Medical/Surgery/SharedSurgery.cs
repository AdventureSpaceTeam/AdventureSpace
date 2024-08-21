using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery;

[Serializable, NetSerializable]
public sealed class SharedPartStatus
{
    public BodyPartType PartType;
    public bool Retracted;
    public bool Incised;
    public bool Opened;
    public bool EndoOpened;
    public bool ExoOpened;

    public SharedPartStatus(BodyPartType partType, bool retracted, bool incised, bool opened, bool endoOpened,
        bool exoOpened)
    {
        PartType = partType;
        Retracted = retracted;
        Incised = incised;
        Opened = opened;
        EndoOpened = endoOpened;
        ExoOpened = exoOpened;
    }
}

[Serializable, NetSerializable]
public sealed class SurgeryBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<BodyPartSlot> BodyPartSlots;
    public List<OrganSlot> OrganSlots;
    public Dictionary<NetEntity, SharedPartStatus> SlotPartsStatus;

    public SurgeryBoundUserInterfaceState(List<BodyPartSlot> bodyPartSlots, List<OrganSlot> organSlots,
        Dictionary<NetEntity, SharedPartStatus> slotPartsStatus)
    {
        BodyPartSlots = bodyPartSlots;
        OrganSlots = organSlots;
        SlotPartsStatus = slotPartsStatus;
    }
}

[Serializable, NetSerializable]
public enum SurgeryUiKey
{
    Key
}

[NetSerializable, Serializable]
public sealed class SurgerySlotButtonPressed : BoundUserInterfaceMessage
{
    public readonly BodyPartSlot Slot;

    public SurgerySlotButtonPressed(BodyPartSlot slot)
    {
        Slot = slot;
    }
}

[NetSerializable, Serializable]
public sealed class OrganSlotButtonPressed : BoundUserInterfaceMessage
{
    public readonly OrganSlot Slot;

    public OrganSlotButtonPressed(OrganSlot slot)
    {
        Slot = slot;
    }
}
