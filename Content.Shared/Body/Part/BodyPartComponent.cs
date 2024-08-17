using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BodyPartComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid? Body;

    [DataField]
    [AutoNetworkedField]
    public BodyPartSlot? BodyPartSlot;

    [DataField]
    [AutoNetworkedField]
    public Dictionary<string, BodyPartSlot> Childs = new();

    [DataField]
    [AutoNetworkedField]
    public Dictionary<string, OrganSlot> Organs = new();

    [DataField]
    [AutoNetworkedField]
    public BodyPartType PartType = BodyPartType.Other;

    [DataField]
    [AutoNetworkedField]
    public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;

    /// <summary>
    ///     Whether or not the owning <see cref="Body"/> will die if all
    ///     <see cref="BodyComponent"/>s of this type are removed from it.
    /// </summary>
    [DataField("vital")]
    [AutoNetworkedField]
    public bool IsVital;

}
