using Content.Shared.Alert;
using Content.Shared.Random;
using Content.Shared.Silicons.Laws;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.AdventureSpace.Roles.StationAI.Components;

[NetworkedComponent]
[RegisterComponent]
public sealed partial class StationAIGhostComponent : Component
{
    [DataField]
    public EntProtoId DoorBolt = "ActionStationAIDoorBolt";

    [DataField]
    public EntityUid? DoorBoltEntity;

    [DataField]
    public EntProtoId DoorOpen = "ActionStationAIDoorOpenClose";

    [DataField]
    public EntityUid? DoorOpenEntity;

    [DataField]
    public EntProtoId DoorEmergency = "ActionStationAIDoorEmergencyAccess";

    [DataField]
    public EntityUid? DoorEmergencyEntity;

    [DataField]
    public EntProtoId DoorElectrify = "ActionStationAIDoorElectrify";

    [DataField]
    public EntityUid? DoorElectrifyEntity;

    [DataField]
    public EntProtoId AIVisitAction = "ActionStationAIVisitAction";

    [DataField]
    public EntityUid? AIVisitEntity;

    [DataField("lawsId")]
    public ProtoId<WeightedRandomPrototype> LawsId = "LawsStationAIDefault";

    public SiliconLawsetPrototype? SelectedLaw;

    [DataField]
    public EntityUid CoreUid = EntityUid.Invalid;

    [DataField]
    public EntityUid ActiveCamera = EntityUid.Invalid;

    [DataField]
    public EntityUid VisitingEntity = EntityUid.Invalid;

    [DataField]
    public ProtoId<AlertPrototype> BorgBatteryNone = "BorgBatteryNone";

    public ProtoId<AlertPrototype> BorgBattery = "BorgBattery";
}
