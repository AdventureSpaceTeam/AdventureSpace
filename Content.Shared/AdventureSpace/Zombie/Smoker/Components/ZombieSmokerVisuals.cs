using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Zombie.Smoker.Components;

[Serializable, NetSerializable]
public enum ZombieSmokerVisuals : byte
{
    Cuffed
}

[Serializable, NetSerializable]
public enum ZombieSmokerState : byte
{
    Idle,
    Prepare,
    Attack
}
