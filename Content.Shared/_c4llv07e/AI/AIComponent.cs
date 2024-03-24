using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._c4llv07e.AI;

[RegisterComponent, NetworkedComponent]
public sealed partial class AIComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<EntityPrototype>> ItemsInHands = new();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class AIControlledComponent : Component {}
