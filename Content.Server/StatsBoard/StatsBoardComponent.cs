namespace Content.Server.StatsBoard
{
    [RegisterComponent, Access(typeof(StatsBoardSystem))]
    public sealed partial class StatsBoardComponent : Component
    {

    }
}

[Serializable]
public sealed partial class StatisticEntries
{
    public List<StatisticEntry> Entries = new();
}

[Serializable]
public sealed partial class StatisticEntry
{
    public string Name { get; set; }
    public int TotalTakeDamage { get; set; } = 0;
    public int TotalTakeHeal { get; set; } = 0;
    public int TotalInflictedDamage { get; set; } = 0;
    public int TotalInflictedHeal { get; set; } = 0;
    public int SlippedCount { get; set; } = 0;
    public int CreamedCount { get; set; } = 0;
    public int DoorEmagedCount { get; set; } = 0;
    public int ElectrocutedCount { get; set; } = 0;
    public int CuffedCount { get; set; } = 0;
    public int AbsorbedPuddleCount { get; set; } = 0;
    public int? SpentTk { get; set; } = null;
    public int DeadCount { get; set; } = 0;
    public int HumanoidKillCount { get; set; } = 0;
    public int KilledMouseCount { get; set; } = 0;
    public TimeSpan CuffedTime { get; set; } = TimeSpan.Zero;
    public TimeSpan SpaceTime { get; set; } = TimeSpan.Zero;
    public TimeSpan SleepTime { get; set; } = TimeSpan.Zero;
    public bool IsInteractedCaptainCard { get; set; } = false;
    public StatisticEntry(string name)
    {
        Name = name;
    }
}
