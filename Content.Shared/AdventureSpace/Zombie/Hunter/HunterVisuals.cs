using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Zombie.Hunter;

[Serializable, NetSerializable]
public enum HunterVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum HunterAttackState : byte
{
    Idle,
    Prepare,
    Fly,
    Attack
}
