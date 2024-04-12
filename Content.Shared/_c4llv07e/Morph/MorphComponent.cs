using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._c4llv07e.Morph;

[RegisterComponent, NetworkedComponent]
public sealed partial class MorphComponent : Component {}

[Serializable, NetSerializable]
public enum MorphVisuals : byte
{
    ResPath,
    StateId,
}

[Serializable, NetSerializable]
public sealed class MorphEvent : EntityEventArgs
{
    public readonly NetEntity EntityUid;
    public readonly ResPath Path;
    public readonly string State;
    public MorphEvent(NetEntity entityUid, ResPath path, string state)
    {
        EntityUid = entityUid;
        Path = path;
        State = state;
    }
}
