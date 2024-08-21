using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.DarkForces.Narsi.Cultist.FireArms;

[Serializable, NetSerializable]
public enum NarsiCultistFireArmsStatus : byte
{
    Status
}

[Serializable, NetSerializable]
public enum NarsiCultistFireArmsState : byte
{
    Fire,
    Empty
}
