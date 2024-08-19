using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.AdventureSpace.DarkForces.Vampire.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireTrallComponent : Component
{
    [DataField("ownerUid")]
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public EntityUid OwnerUid;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "VampireTrallIcon";

    [DataField]
    public SoundSpecifier Alert = new SoundPathSpecifier("/Audio/DarkStation/DarkForces/Vampire/vampalert.ogg");
}
