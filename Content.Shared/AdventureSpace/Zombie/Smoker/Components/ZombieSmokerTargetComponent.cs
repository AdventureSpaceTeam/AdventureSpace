using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.Zombie.Smoker.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ZombieSmokerTargetComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid Smoker = EntityUid.Invalid;

    [DataField]
    public SoundSpecifier Sound =  new SoundPathSpecifier("/Audio/DarkStation/Mobs/Zombie/Smoker/launchtongue.ogg");
}
