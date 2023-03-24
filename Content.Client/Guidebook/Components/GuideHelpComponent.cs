using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Client.Guidebook.Components;

/// <summary>
/// This component stores a reference to a guidebook that contains information relevant to this entity.
/// </summary>
[RegisterComponent]
[Access(typeof(GuidebookSystem))]
public sealed class GuideHelpComponent : Component
{
    /// <summary>
    /// What guides to include show when opening the guidebook. The first entry will be used to select the currently
    /// selected guidebook.
    /// </summary>
    [DataField("guides", customTypeSerializer: typeof(PrototypeIdListSerializer<GuideEntryPrototype>), required: true)]
    [ViewVariables]
    public List<string> Guides = new();

    /// <summary>
    /// Whether or not to automatically include the children of the given guides.
    /// </summary>
    [DataField("includeChildren")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IncludeChildren = true;

    /// <summary>
    /// Whether or not to open the UI when interacting with the entity while on hand.
    /// Mostly intended for books
    /// </summary>
    [DataField("openOnActivation")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool OpenOnActivation;
}
