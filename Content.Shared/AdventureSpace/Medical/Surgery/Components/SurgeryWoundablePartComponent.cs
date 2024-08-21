using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SurgeryWoundablePartComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public float Damage;

    [DataField(required:true)]
    [AutoNetworkedField]
    public float MaxDamage;

    [DataField]
    [AutoNetworkedField]
    public bool CanBeExplosionTarget;

    [DataField]
    [AutoNetworkedField]
    public bool CanBeDollTarget;
}
