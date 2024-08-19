using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Roles.CCO;

[Serializable] [NetSerializable]
public sealed class CcoConsoleStationState(
    CcoConsoleAlert alertState,
    CcoConsoleEmergencyShuttle emergencyShuttle,
    CcoConsoleSpecialSquadModel specialSquads,
    CcoConsoleSalaries salaries,
    string stationName
)
{
    public CcoConsoleAlert AlertState = alertState;
    public CcoConsoleEmergencyShuttle EmergencyShuttle = emergencyShuttle;
    public CcoConsoleSalaries Salaries = salaries;
    public CcoConsoleSpecialSquadModel Squads = specialSquads;
    public string StationName = stationName;
}

[Serializable] [NetSerializable]
public sealed class CcoConsoleAlert(
    string alertName,
    Color alertColor,
    string alertDescription
)
{
    public Color AlertColor = alertColor;
    public string AlertDescription = alertDescription;
    public string AlertName = alertName;
}

[Serializable] [NetSerializable]
public sealed class CcoConsoleEmergencyShuttle(
    EmergencyShuttleState shuttle,
    TimeSpan? expectedCountDownEnd
)
{
    public TimeSpan? ExpectedCountDownEnd = expectedCountDownEnd;
    public EmergencyShuttleState ShuttleState = shuttle;
}

[Serializable] [NetSerializable]
public sealed class CcoConsoleSalaries(Dictionary<uint, GeneralStationRecord>? records)
{
    public Dictionary<uint, GeneralStationRecord>? Records { get; } = records;
}
