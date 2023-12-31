using Content.Shared.CallErt;
using Robust.Shared.Prototypes;

namespace Content.Server.CallErt;

[Prototype("ertGroups")]
public sealed class ErtGroupsPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("groups")] public Dictionary<string, ErtGroupDetail> ErtGroupList = new();
}
