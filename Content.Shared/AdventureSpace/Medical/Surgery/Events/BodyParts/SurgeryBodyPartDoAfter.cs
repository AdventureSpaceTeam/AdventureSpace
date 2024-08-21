using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Events.BodyParts;

[Serializable, NetSerializable]
public partial class SurgeryBodyPartDoAfter : DoAfterEvent
{
    public SurgeryBodyPartModel Model;

    public SurgeryBodyPartDoAfter(SurgeryBodyPartModel model)
    {
        Model = model;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class SurgerySawExoBody : SurgeryBodyPartDoAfter
{
    public SurgerySawExoBody(SurgeryBodyPartModel model) : base(model)
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgerySawEndoBody : SurgeryBodyPartDoAfter
{
    public SurgerySawEndoBody(SurgeryBodyPartModel model) : base(model)
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgerySawRemoveBodyPart : SurgeryBodyPartDoAfter
{
    public SurgerySawRemoveBodyPart(SurgeryBodyPartModel model) : base(model)
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgeryAttachBodyPart : SurgeryBodyPartDoAfter
{
    public NetEntity NewBodyPartUid;

    public SurgeryAttachBodyPart(SurgeryBodyPartModel model, NetEntity newBodyPartUid) : base(model)
    {
        NewBodyPartUid = newBodyPartUid;
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgeryHardSutureEndoPart : SurgeryBodyPartDoAfter
{
    public SurgeryHardSutureEndoPart(SurgeryBodyPartModel model) : base(model)
    {

    }
}

[Serializable, NetSerializable]
public sealed partial class SurgeryHardSutureExoPart : SurgeryBodyPartDoAfter
{
    public SurgeryHardSutureExoPart(SurgeryBodyPartModel model) : base(model)
    {

    }
}
