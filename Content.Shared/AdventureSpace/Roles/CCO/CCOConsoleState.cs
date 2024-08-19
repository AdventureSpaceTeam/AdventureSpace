using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Roles.CCO;

[Serializable] [NetSerializable]
public enum CcoConsoleInterfaceKey
{
    Key,
}

[Serializable] [NetSerializable]
public sealed class CcoConsoleUIState(
    CcoConsoleAvailableStations stations,
    CcoConsoleBase consoleBase
) : BoundUserInterfaceState
{
    public CcoConsoleAvailableStations Stations = stations;
    public CcoConsoleBase ConsoleBase = consoleBase;
}

[Serializable] [NetSerializable]
public sealed class CcoConsoleBase(
    NetEntity? selectedStation,
    string operatorName
)
{
    public NetEntity? SelectedStation = selectedStation;
    public string OperatorName = operatorName;
}

[Serializable, NetSerializable]
public sealed class CcoConsoleAvailableStations(Dictionary<NetEntity, CcoConsoleStationState> availableStations)
{
    public Dictionary<NetEntity, CcoConsoleStationState> AvailableStation = availableStations;
}

[Serializable] [NetSerializable]
public enum EmergencyShuttleState
{
    Idle,
    OnWay,
    Arrived,
    OnWayCentcom,
    Unknown,
}
