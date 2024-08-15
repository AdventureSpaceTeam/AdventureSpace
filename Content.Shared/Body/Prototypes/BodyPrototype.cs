using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Body.Prototypes;

[Prototype("body")]
public sealed partial class BodyPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = "";

    [DataField("root")] public string Root { get; private set; } = string.Empty;

    [DataField("slots")] public Dictionary<string, BodyPrototypeSlot> Slots { get; private set; } = new();

    private BodyPrototype() { }

    public BodyPrototype(string id, string name, string root, Dictionary<string, BodyPrototypeSlot> slots)
    {
        ID = id;
        Name = name;
        Root = root;
        Slots = slots;
    }
}

[DataRecord]
public sealed record BodyPrototypeSlot
{
    public readonly EntProtoId? Part;
    public readonly HashSet<string> Connections = new();
    public readonly Dictionary<string, OrganPrototypeSlot> Organs = new();

    //defines is a slot if empty, while still allowing
    [DataField("slotType")]
    public readonly BodyPartType? SlotType = new();

    public BodyPrototypeSlot(string? part, HashSet<string>? connections, Dictionary<string, OrganPrototypeSlot>? organs, BodyPartType? slotType)
    {
        Part = part;
        Connections = connections ?? new HashSet<string>();
        Organs = organs ?? new Dictionary<string, OrganPrototypeSlot>();
        SlotType = slotType;
    }

    public void Deconstruct(out string? part, out HashSet<string> connections, out Dictionary<string, OrganPrototypeSlot> organs)
    {
        part = Part;
        connections = Connections;
        organs = Organs;
    }
}

[DataRecord]
public sealed record OrganPrototypeSlot
{
    [DataField("organ", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public readonly string? Organ;

    [DataField("type")]
    public readonly OrganType SlotType = new();

    [DataField("internal")]
    public readonly bool Internal = true;

    public OrganPrototypeSlot(string? organ, OrganType slotType, bool internalOrgan)
    {
        Organ = organ;
        SlotType = slotType;
        Internal = internalOrgan;
    }

    public void Deconstruct(out string? organ, bool internalOrgan)
    {
        organ = Organ;
        internalOrgan = Internal;
    }
}
