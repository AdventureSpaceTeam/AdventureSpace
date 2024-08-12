using Robust.Shared.Serialization;

namespace Content.Shared.CallErt;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ErtGroupDetail
{
    [DataField("name")]
    public string Name { get; set; } = string.Empty;

    [DataField("announcement")]
    public bool Announcement { get; set; } = true;

    [DataField("showInConsole")]
    public bool ShowInConsole { get; set; } = true;

    [DataField("shuttle")]
    public string ShuttlePath = "Maps/Shuttles/med_ert_shuttle.yml";

    [DataField("humansList")]
    public Dictionary<string, int> HumansList = new ();

    [DataField("waitingTime")]
    public float WaitingTime = 600;

    [DataField("requirements")]
    public Dictionary<string, int> Requirements = new ();

    [DataField("shuttleTime")] public TimeSpan ShuttleTime { get; set; } = TimeSpan.FromMinutes(10);
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class CallErtGroupEnt
{
    public string? Id;
    public ErtGroupStatus Status;
    public TimeSpan CalledTime;
    public TimeSpan ArrivalTime;
    public TimeSpan ReviewTime;
    public string? Reason;
    public ErtGroupDetail? ErtGroupDetail;
}

[Serializable, NetSerializable]
public enum ErtGroupStatus
{
    Approved,
    Denied,
    Waiting,
    Arrived,
    Revoke,
}

[Serializable, NetSerializable]
public enum ApproveErtConsoleUiKey
{
    Key
}
