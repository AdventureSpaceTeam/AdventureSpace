using Content.Shared.Actions;

namespace Content.Shared.AdventureSpace.Vampire;

public interface IVampireEvent
{
    public int BloodCost { get; }
}

public sealed partial class VampireOpenHelpDialogEvent : InstantActionEvent
{
}

public sealed partial class VampireDrinkBloodAblityEvent : EntityTargetActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireRejuvenateEvent : InstantActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireFlashEvent : InstantActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireHypnosisEvent : EntityTargetActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireChargeEvent : WorldTargetActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireShapeshiftEvent : InstantActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampirePolymorphEvent : InstantActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireRejuvenatePlusEvent : InstantActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireSummonBatsEvent : InstantActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireChiropteanScreechEvent : InstantActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireEnthrallEvent : EntityTargetActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}

public sealed partial class VampireFullPowerEvent : InstantActionEvent, IVampireEvent
{
    [DataField]
    public int BloodCost { get; private set; }
}
