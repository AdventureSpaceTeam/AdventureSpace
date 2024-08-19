using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Events;

[Serializable, NetSerializable]
public sealed partial class SurgeryRemoveToolDoAfter : DoAfterEvent
{
    public NetEntity BodyPart;

    public SurgeryRemoveToolDoAfter(NetEntity bodyPart)
    {
        BodyPart = bodyPart;
    }

    public override DoAfterEvent Clone() => this;
}
