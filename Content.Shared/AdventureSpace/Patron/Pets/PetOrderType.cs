using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Patron.Pets;

[Serializable, NetSerializable]
public enum PetOrderType : byte
{
    Stay,
    Follow,
    Attack
}
