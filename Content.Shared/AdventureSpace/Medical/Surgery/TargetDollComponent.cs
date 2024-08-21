using Content.Shared.Body.Part;
using Robust.Shared.GameStates;

namespace Content.Shared.AdventureSpace.Medical.Surgery;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TargetDollComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public BodyPartType? TargetBodyPart;

    [DataField]
    [AutoNetworkedField]
    public BodyPartSymmetry BodyPartSymmetry = BodyPartSymmetry.None;
}
