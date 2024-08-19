using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Events.Doll;

[Serializable]
[NetSerializable]
public sealed class TargetDollChangedEvent : EntityEventArgs
{
    public NetEntity Entity;
    public BodyPartType? Type;
    public BodyPartSymmetry Symmetry;
}
