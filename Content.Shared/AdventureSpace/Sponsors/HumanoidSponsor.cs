using Robust.Shared.Serialization;

namespace Content.Shared.SecretStation.Sponsors;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidSponsorData
{
    [DataField]
    public ProfilePetData PetData = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ProfilePetData
{
    [DataField]
    public string PetId = default!;

    [DataField]
    public string PetName = default!;
}
