﻿using Content.Shared.Body.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
[DataRecord]
public sealed record OrganSlot(string Id, NetEntity Parent, OrganType? Type, bool Internal)
{
    public NetEntity? Child { get; set; }

    /// <summary>
    /// an attached surgical tool on the body part slot (such as a Torniquet)
    /// </summary>
    public NetEntity? Attachment { get; set; }

    public bool Cauterised = false;

    // Rider doesn't suggest explicit properties during deconstruction without this
    public void Deconstruct(out NetEntity? child, out string id, out NetEntity parent, out NetEntity? attachment, out bool cauterised, OrganType? type, bool internalOrgan)
    {
        child = Child;
        id = Id;
        parent = Parent;
        attachment = Attachment;
        cauterised = Cauterised;
        type = Type;
        internalOrgan = Internal; // where an organ slot is internal or not - external organs in these slots can be accessed without having to open the containing body part
    }
}
