using Content.Shared.Actions;

namespace Content.Shared.AdventureSpace.Hunter.Desecrated.Pontific;

public interface IPontificAction
{
    public int FelCost { get; set; }
}

public sealed partial class PontificBloodyAltarEvent : InstantActionEvent, IPontificAction
{
    [DataField]
    public int FelCost { get; set; }
}

public sealed partial class PontificDarkPrayerEvent : InstantActionEvent, IPontificAction
{
    [DataField]
    public int FelCost { get; set; }
}

public sealed partial class PontificFelLightningEvent : EntityTargetActionEvent, IPontificAction
{
    [DataField]
    public int FelCost { get; set; }
}

public sealed partial class PontificFlameSwordsEvent : InstantActionEvent, IPontificAction
{
    [DataField]
    public int FelCost { get; set; }
}

public sealed partial class PontificLungeOfFaithEvent : InstantActionEvent, IPontificAction
{
    [DataField]
    public int FelCost { get; set; }
}

public sealed partial class PontificSpawnGuardianEvent : InstantActionEvent, IPontificAction
{
    [DataField]
    public int FelCost { get; set; }
}

public sealed partial class PontificSpawnMonkEvent : InstantActionEvent, IPontificAction
{
    [DataField]
    public int FelCost { get; set; }
}

public sealed partial class PontificKudzuEvent : InstantActionEvent, IPontificAction
{
    [DataField]
    public int FelCost { get; set; }
}
