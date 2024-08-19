using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Hunter.Desecrated.Pontific;

[Serializable, NetSerializable]
public enum PontificState : byte
{
    Base,
    Dead,
    Flame,
    // Prayer
}

[Serializable, NetSerializable]
public enum PontificStateVisuals : byte
{
    State
}

