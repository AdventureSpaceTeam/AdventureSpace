using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Patron.Ghost;

[Serializable, NetSerializable]
public sealed class SponsorChangeGhostEvent(string id) : BoundUserInterfaceMessage
{
    public string Id = id;
}

[Serializable, NetSerializable]
public enum SponsorGhostInterfaceKey
{
    Key,
}
