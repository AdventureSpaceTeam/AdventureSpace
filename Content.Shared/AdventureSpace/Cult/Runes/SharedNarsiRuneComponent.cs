using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Cult.Runes;

public abstract partial class SharedNarsiRuneComponent : Component
{
    [Serializable, NetSerializable]
    public enum RuneStatus : byte
    {
        Status
    }

    [Serializable, NetSerializable]
    public enum RuneState : byte
    {
        Idle,
        Active
    }
}
