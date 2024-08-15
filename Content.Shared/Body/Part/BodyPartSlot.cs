using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[Serializable, NetSerializable]
[ImplicitDataRecord]
public sealed record BodyPartSlot(string Id, NetEntity Parent, BodyPartType? Type, BodyPartSymmetry Symmetry)
{
    /// <summary>
    /// the body part occupying the slot
    /// </summary>
    public NetEntity? BodyPart { get; set; }

    /// <summary>
    /// an attached surgical tool on the body part slot (such as a Torniquet)
    /// </summary>
    public NetEntity? Attachment { get; set; }

    public bool Cauterised;
}
