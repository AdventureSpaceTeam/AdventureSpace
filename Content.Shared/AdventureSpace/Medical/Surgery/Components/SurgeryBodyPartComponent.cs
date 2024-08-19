using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryBodyPartComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public BodyPartVisuals Visuals = new();

    [DataField]
    [AutoNetworkedField]
    public BodyPartState State = new();

    [DataField(required: true)]
    public string Species = "";

    [DataField]
    public bool Container;
}

[DataRecord]
[Serializable, NetSerializable]
public sealed record BodyPartState
{
    [DataField]
    public NetEntity? Attachment;

    [DataField]
    public bool Incisable;

    [DataField]
    public bool Incised;

    [DataField]
    public bool Opened;

    [DataField]
    public bool EndoSkeleton;

    [DataField]
    public bool ExoSkeleton;

    [DataField]
    public bool EndoOpened;

    [DataField]
    public bool ExoOpened;
}

[DataRecord]
[Serializable, NetSerializable]
public sealed record BodyPartVisuals
{
    [DataField(required: true)]
    public ProtoId<HumanoidSpeciesSpriteLayer>? SpeciesSprite;

    [DataField]
    public Dictionary<MarkingCategories, List<Marking>> Markings = new();

    [DataField]
    public Color? Color;

    [DataField]
    public bool OverrideColor;

    public CustomBaseLayerInfo ToLayerInfo()
    {
        return new CustomBaseLayerInfo(SpeciesSprite, Color != null || OverrideColor, Color);
    }
}
