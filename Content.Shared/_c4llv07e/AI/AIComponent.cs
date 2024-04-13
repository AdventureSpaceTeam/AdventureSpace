using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._c4llv07e.AI;

[RegisterComponent, NetworkedComponent]
public sealed partial class AIComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<EntityPrototype>> ItemsInHands = new();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsVisible = false;
}

[Serializable, NetSerializable]
public enum AIVisuals
{
    Visibility,
}

[RegisterComponent, NetworkedComponent]
public sealed partial class AIControlledComponent : Component {}

public sealed partial class AIToggleVisibilityActionEvent : InstantActionEvent { }
