using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.AdventureSpace.Roles.StationAI.Components;

[NetworkedComponent]
[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class StationAIVisitorComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid? AIGhost;

    [DataField]
    public EntProtoId BackToAIGhostAction = "ActionStationAIVisitBackToBody";

    [DataField]
    public EntityUid? ActionBackToAIGhost;
}
