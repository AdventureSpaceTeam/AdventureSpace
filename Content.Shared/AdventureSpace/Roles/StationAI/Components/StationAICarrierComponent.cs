using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.Roles.StationAI.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class StationAICarrierComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public NetEntity? AIGhostEntity;

}
