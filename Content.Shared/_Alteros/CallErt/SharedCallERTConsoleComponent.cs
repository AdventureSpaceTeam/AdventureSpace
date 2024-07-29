using Robust.Shared.Serialization;

namespace Content.Shared.CallErt
{
    [Virtual]
    public partial class SharedCallErtConsoleComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public sealed class CallErtConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly bool CanCallErt;
        public Dictionary<string, ErtGroupDetail> ErtsList;
        public List<CallErtGroupEnt> CalledErtsList;
        public string? SelectedErtGroup;

        public CallErtConsoleInterfaceState(
            bool canCallErt,
            Dictionary<string, ErtGroupDetail> ertsList,
            List<CallErtGroupEnt> calledErtsList,
            string? selectedErtGroup)
        {
            CanCallErt = canCallErt;
            ErtsList = ertsList;
            CalledErtsList = calledErtsList;
            SelectedErtGroup = selectedErtGroup;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ApproveErtConsoleInterfaceState : BoundUserInterfaceState
    {
        public bool CanSendErt;
        public bool AutomaticApprove;
        public Dictionary<string, ErtGroupDetail> ErtsList;
        public List<CallErtGroupEnt> CalledErtsList;
        public Dictionary<NetEntity, string> StationList;
        public NetEntity? SelectedStation;
        public string? SelectedErtGroup;

        public ApproveErtConsoleInterfaceState(bool canSendErt,
            bool automaticApprove,
            Dictionary<string, ErtGroupDetail> ertsList,
            List<CallErtGroupEnt> calledErtsList,
            Dictionary<NetEntity, string> stationList,
            NetEntity? selectedStation,
            string? selectedErtGroup)
        {
            CanSendErt = canSendErt;
            AutomaticApprove = automaticApprove;
            ErtsList = ertsList;
            CalledErtsList = calledErtsList;
            StationList = stationList;
            SelectedStation = selectedStation;
            SelectedErtGroup = selectedErtGroup;
        }
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
    public sealed class CallErtConsoleCallErtMessage : BoundUserInterfaceMessage
    {
        public readonly string ErtGroup;
        public readonly string Reason;

        public CallErtConsoleCallErtMessage(string ertGroup, string reason)
        {
            ErtGroup = ertGroup;
            Reason = reason;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CallErtConsoleSendErtMessage : BoundUserInterfaceMessage
    {
        public readonly NetEntity Station;
        public readonly string ErtGroup;

        public CallErtConsoleSendErtMessage(NetEntity station, string ertGroup)
        {
            Station = station;
            ErtGroup = ertGroup;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CallErtConsoleUpdateMessage : BoundUserInterfaceMessage
    {
        public CallErtConsoleUpdateMessage()
        {
        }
    }


    [Serializable, NetSerializable]
    public sealed class CallErtConsoleRecallErtMessage : BoundUserInterfaceMessage
    {
        public readonly int IndexGroup;

        public CallErtConsoleRecallErtMessage(int indexGroup)
        {
            IndexGroup = indexGroup;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ApproveErtConsoleRecallErtMessage : BoundUserInterfaceMessage
    {
        public readonly NetEntity Station;
        public readonly int IndexGroup;

        public ApproveErtConsoleRecallErtMessage(NetEntity station, int indexGroup)
        {
            Station = station;
            IndexGroup = indexGroup;
        }
    }


    [Serializable, NetSerializable]
    public sealed class CallErtConsoleApproveErtMessage : BoundUserInterfaceMessage
    {
        public readonly int IndexGroup;

        public CallErtConsoleApproveErtMessage(int indexGroup)
        {
            IndexGroup = indexGroup;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CallErtConsoleToggleAutomateApproveErtMessage : BoundUserInterfaceMessage
    {
        public readonly bool AutomateApprove;

        public CallErtConsoleToggleAutomateApproveErtMessage(bool automateApprove)
        {
            AutomateApprove = automateApprove;
        }
    }


    [Serializable, NetSerializable]
    public sealed class CallErtConsoleDenyErtMessage : BoundUserInterfaceMessage
    {
        public readonly int IndexGroup;

        public CallErtConsoleDenyErtMessage(int indexGroup)
        {
            IndexGroup = indexGroup;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CallErtConsoleSelectStationMessage : BoundUserInterfaceMessage
    {
        public NetEntity StationUid;

        public CallErtConsoleSelectStationMessage(NetEntity stationUid)
        {
            StationUid = stationUid;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CallErtConsoleSelectErtMessage : BoundUserInterfaceMessage
    {
        public string ErtGroup;

        public CallErtConsoleSelectErtMessage(string ertGroup)
        {
            ErtGroup = ertGroup;
        }
    }

    [Serializable, NetSerializable]
    public enum CallErtConsoleUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum ApproveErtConsoleUiKey
    {
        Key
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
}
