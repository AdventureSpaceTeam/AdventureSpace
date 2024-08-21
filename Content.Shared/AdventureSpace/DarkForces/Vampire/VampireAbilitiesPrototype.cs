using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.AdventureSpace.DarkForces.Vampire;

[Serializable]
[Prototype("vampireAbility")]
public sealed partial class VampireAbilitiesPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string Description = string.Empty;

    [DataField(required: true)]
    public int BloodCost;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    [DataField(required: true)]
    public EntProtoId ActionId;

    [DataField]
    public EntProtoId? ReplaceId;
}
