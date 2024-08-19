using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.AdventureSpace.DarkForces.Vampire.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class VampireComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int CurrentBloodAmount;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int TotalDrunkBlood;

    [DataField]
    public bool FullPower;

    /** Default Actions **/
    [DataField]
    public EntProtoId ActionStatistic = "VampireStatisticAction";

    [DataField]
    public EntityUid? ActionStatisticEntity;

    [DataField]
    public EntProtoId ActionDrinkBlood = "VampireDrinkBloodAction";

    [DataField]
    public EntityUid? ActionDrinkBloodEntity;

    [DataField]
    public EntProtoId ActionRejuvenate = "VampireRejuvenateAction";

    [DataField]
    public EntProtoId ActionFlash = "VampireFlashAction";

    [DataField]
    public EntityUid? ActionFlashEntity;

    [DataField]
    public EntityUid? ActionRejuvenateEntity;

    [DataField]
    public EntProtoId ActionFullPower = "VampireFullPowerAction";

    [DataField]
    public EntityUid? ActionFullPowerEntity;

    /** End Default Actions **/
    [DataField]
    public Dictionary<string, EntityUid> OpenedAbilities = new();

    /** Objectives **/
    [DataField(customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan NextObjectivesCheckTick;

    public TimeSpan ObjectivesCheckPeriod = TimeSpan.FromSeconds(30);

    [DataField]
    public List<EntityUid> Objectives = new();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "VampireIcon";
}
