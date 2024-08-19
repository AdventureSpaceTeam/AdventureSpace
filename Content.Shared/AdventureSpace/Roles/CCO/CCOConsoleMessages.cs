using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Roles.CCO;

[Serializable, NetSerializable]
public sealed class CcoConsoleSendAnnouncementMessage(string message) : BoundUserInterfaceMessage
{
    public readonly string Message = message;
}

[Serializable, NetSerializable]
public sealed class CcoConsoleOpenCrewManifestMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CcoConsoleSendSpecialSquadMessage(string squadId) : BoundUserInterfaceMessage
{
    public readonly string SquadId = squadId;
}

[Serializable, NetSerializable]
public sealed class CcoConsoleSendEmergencyShuttleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CcoConsoleCancelEmergencyShuttleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CcoConsoleCrewMemberSalaryBonusMessage(int bonus, uint record) : BoundUserInterfaceMessage
{
    public readonly int Bonus = bonus;
    public readonly uint Record = record;
}

[Serializable, NetSerializable]
public sealed class CcoConsoleCrewMemberSalaryPenaltyMessage(int penalty, uint record) : BoundUserInterfaceMessage
{
    public readonly int Penalty = penalty;
    public readonly uint Record = record;
}

[Serializable, NetSerializable]
public sealed class CcoConsoleStationSelected(NetEntity station) : BoundUserInterfaceMessage
{
    public readonly NetEntity Station = station;
}
