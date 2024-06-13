using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._c4llv07e.Aliens;

[RegisterComponent, NetworkedComponent]
public sealed partial class FaceHuggerComponent : Component
{
    [DataField] public EntityUid? Action;
}

public sealed partial class FaceJumpInstantActionEvent : InstantActionEvent {}
