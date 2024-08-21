using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.DarkForces.Ratvar.Righteous.Abilities;

[Serializable, NetSerializable]
public enum RatvarEnchantmentableVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum RatvarEnchantmentableOverlays : byte
{
    None,
    KnockOff,
    Stun,
    Doors,
    Walls,
    Teleport,
    Heal,
    Hidings,
    Confusion,
    ElectricalTouch,
    Swordsman,
    Bloodshed
}
