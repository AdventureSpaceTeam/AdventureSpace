using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Surgery.Events.Organs;

[Serializable, NetSerializable]
public partial class SurgeryOrganDoAfter : DoAfterEvent
{
    public SurgeryOrganModel Model;

    public SurgeryOrganDoAfter(SurgeryOrganModel model)
    {
        Model = model;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class SurgerySutureOrgan : SurgeryOrganDoAfter
{
    public NetEntity NewOrgan;

    public SurgerySutureOrgan(SurgeryOrganModel model, NetEntity newOrgan) : base(model)
    {
        NewOrgan = newOrgan;
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgeryRemoveOrgan : SurgeryOrganDoAfter
{
    public SurgeryRemoveOrgan(SurgeryOrganModel model) : base(model)
    {

    }
}
