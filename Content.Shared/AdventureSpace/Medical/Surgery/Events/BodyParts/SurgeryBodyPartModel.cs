using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Events.BodyParts;

[Serializable, NetSerializable]
public record SurgeryBodyPartModel(
    NetEntity User,
    NetEntity Target,
    NetEntity Tool,
    BodyPartSlot Slot
);

