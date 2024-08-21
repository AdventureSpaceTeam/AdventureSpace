using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.AdventureSpace.Zombie.Smoker.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ZombieSmokerComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid CurrentTarget = EntityUid.Invalid;

    [DataField]
    [AutoNetworkedField]
    public ZombieSmokerState SmokerState = ZombieSmokerState.Idle;

    [DataField, ViewVariables]
    public SpriteSpecifier Tongue = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Reeling;

    [DataField]
    public SoundSpecifier AttackSound = default!;

    [DataField]
    public TimeSpan SmokerPrepareOffset;

}
