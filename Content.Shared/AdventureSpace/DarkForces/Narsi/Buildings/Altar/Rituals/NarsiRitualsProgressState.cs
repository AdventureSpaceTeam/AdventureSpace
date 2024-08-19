using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.DarkForces.Narsi.Buildings.Altar.Rituals;

[Serializable, NetSerializable]
public enum NarsiRitualsProgressState
{
    Idle,
    Working,
    Delay
}
