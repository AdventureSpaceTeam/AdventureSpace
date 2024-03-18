using System.Diagnostics.CodeAnalysis;
using Content.Server._Sunrise.Shuttles;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Revolutionary.Components;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Atmos.Monitor;
using Content.Shared.CallErt;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NukeOps;
using Content.Shared.Zombies;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.CallErt;

public sealed class StationCallErtSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
    }

    private void OnStationInitialize(StationInitializedEvent args)
    {
        if (!TryComp<StationCallErtComponent>(args.Station, out var callErtComponent))
            return;

        if (!_prototypeManager.TryIndex(callErtComponent.ErtGroupsPrototype, out ErtGroupsPrototype? ertGroups))
        {
            return;
        }

        callErtComponent.ErtGroups = ertGroups;
    }


    private bool TryAddShuttle(string shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleGrid)
    {
        shuttleGrid = null;
        var shuttleMap = _mapManager.CreateMap();

        if (!_map.TryLoad(shuttleMap, shuttlePath, out var gridList))
        {
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
            return false;
        }

        if (gridList.Count != 1)
        {
            switch (gridList.Count)
            {
                case < 1:
                    _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, no grid found in file");
                    break;
                case > 1:
                {
                    _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, too many grids present in file");

                    foreach (var grid in gridList)
                    {
                        _mapManager.DeleteGrid(grid);
                    }

                    break;
                }
            }

            return false;
        }

        shuttleGrid = gridList[0];
        EnsureComp<ErtShuttleComponent>(shuttleGrid.Value);
        return true;
    }


    private bool CheckCallErt(EntityUid stationUid, CallErtGroupEnt ertGroupEnt, StationCallErtComponent component)
    {
        var curTime = _gameTiming.CurTime;

        var totalHumans = GetHumans(stationUid).Count;
        var totalZombies = GetZombies(stationUid).Count;
        var totalDeadHumans = GetDeadHumans(stationUid).Count;
        var totalPudles = GetPuddles(stationUid).Count;
        var totalDangerAtmos = GetDangerAtmos(stationUid).Count;
        var liveNukiesOnStation = GetNukiesOnStation(stationUid).Count;
        var totalDeadHeads = GetDeadHeads(stationUid).Count;
        var roundDuration = curTime.TotalMinutes;

        if (ertGroupEnt.ErtGroupDetail == null)
            return false;

        foreach (var calledErtGroup in component.CalledErtGroups)
        {
            if (calledErtGroup.Id != ertGroupEnt.Id)
                continue;

            if (calledErtGroup.Status is not (ErtGroupStatus.Arrived or ErtGroupStatus.Approved))
                continue;

            if (curTime < calledErtGroup.ArrivalTime + TimeSpan.FromSeconds(component.TimeoutNewApprovedCall))
            {
                return false;
            }
        }

        if (ertGroupEnt.ErtGroupDetail.Requirements.TryGetValue("RoundDuration", out var requirementDuration))
        {
            if (roundDuration > requirementDuration)
            {
                return true;
            }
        }

        if (ertGroupEnt.ErtGroupDetail.Requirements.TryGetValue("DeadPercent", out var requirementDead))
        {
            var deadPercent = totalDeadHumans / (float)totalHumans * 100;
            if (deadPercent >= requirementDead)
                return true;
        }

        if (ertGroupEnt.ErtGroupDetail.Requirements.TryGetValue("DeadHeads", out var requirementDeadHeads))
        {
            if (totalDeadHeads >= requirementDeadHeads)
                return true;
        }

        if (ertGroupEnt.ErtGroupDetail.Requirements.TryGetValue("ZombiePercent", out var requirementZombie))
        {
            var zombiePercent = totalZombies / (float)totalHumans * 100;
            if (zombiePercent >= requirementZombie)
                return true;
        }

        if (ertGroupEnt.ErtGroupDetail.Requirements.TryGetValue("PuddlesCount", out var requirementPudles))
        {
            if (totalPudles >= requirementPudles)
                return true;
        }

        if (ertGroupEnt.ErtGroupDetail.Requirements.TryGetValue("DangerAlarmCount", out var requirementAlarms))
        {
            if (totalDangerAtmos >= requirementAlarms)
                return true;
        }

        if (ertGroupEnt.ErtGroupDetail.Requirements.TryGetValue("NukiesOnStation", out var requirementNukiesOnStation))
        {
            if (liveNukiesOnStation >= requirementNukiesOnStation)
                return true;
        }

        return false;
    }

    private List<EntityUid> GetNukiesOnStation(EntityUid stationUid)
    {
        var liveNukies = new List<EntityUid>();
        var nukeOperativeQuery = AllEntityQuery<NukeOperativeComponent, MobStateComponent>();
        while (nukeOperativeQuery.MoveNext(out var uid, out var nukeOp, out var stateComponent))
        {
            if (stateComponent.CurrentState == MobState.Dead)
                continue;
            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid == stationUid)
                liveNukies.Add(uid);
        }
        return liveNukies;
    }

    private List<EntityUid> GetHumans(EntityUid stationUid)
    {
        var humans = new List<EntityUid>();
        var humansQuery = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();
        while (humansQuery.MoveNext(out var uid, out _, out var mob))
        {
            if (!_mobState.IsAlive(uid, mob))
                continue;

            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid == stationUid)
                humans.Add(uid);
        }
        return humans;
    }


    private List<EntityUid> GetZombies(EntityUid stationUid)
    {
        var healthy = new List<EntityUid>();
        var zombies = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent, ZombieComponent>();
        while (zombies.MoveNext(out var uid, out _, out var mob, out var zombie))
        {
            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid == stationUid)
                healthy.Add(uid);
        }
        return healthy;
    }


    private List<EntityUid> GetPuddles(EntityUid stationUid)
    {
        var pudles = new List<EntityUid>();
        var pudlesQuery = AllEntityQuery<PuddleComponent>();
        while (pudlesQuery.MoveNext(out var uid, out var puddle))
        {
            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid == stationUid)
                pudles.Add(uid);
        }
        return pudles;
    }


    private List<EntityUid> GetDangerAtmos(EntityUid stationUid)
    {
        var dangerAtmos = new List<EntityUid>();
        var atmosAlarmableQuery = AllEntityQuery<AtmosAlarmableComponent>();
        while (atmosAlarmableQuery.MoveNext(out var uid, out var atmosAlarmable))
        {
            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid != stationUid)
                continue;
            if (atmosAlarmable.LastAlarmState == AtmosAlarmType.Danger)
                dangerAtmos.Add(uid);
        }
        return dangerAtmos;
    }


    private List<EntityUid> GetDeadHeads(EntityUid stationUid)
    {
        var deadheads = new List<EntityUid>();
        var players = AllEntityQuery<CommandStaffComponent, MobStateComponent>();
        while (players.MoveNext(out var uid, out _, out var mob))
        {
            if (!_mobState.IsDead(uid, mob))
                continue;

            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid == stationUid)
                deadheads.Add(uid);
        }
        return deadheads;
    }

    private List<EntityUid> GetDeadHumans(EntityUid stationUid)
    {
        var deadHumans = new List<EntityUid>();
        var players = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();
        while (players.MoveNext(out var uid, out _, out var mob))
        {
            if (!_mobState.IsDead(uid, mob))
                continue;

            var owningStationUid = _stationSystem.GetOwningStation(uid);
            if (owningStationUid == stationUid)
                deadHumans.Add(uid);
        }
        return deadHumans;
    }


    public void CallErt(EntityUid stationUid, string ertGroup, string reason, MetaDataComponent? dataComponent = null, StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null
            || !component.ErtGroups.ErtGroupList.TryGetValue(ertGroup, out var ertGroupDetails))
            return;

        var curTime = _gameTiming.CurTime;

        var ertGroupEnt = new CallErtGroupEnt
        {
            Id = ertGroup,
            CalledTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan),
            Status = ErtGroupStatus.Waiting,
            ArrivalTime = TimeSpan.Zero,
            ReviewTime = curTime + TimeSpan.FromSeconds(component.ReviewTime),
            Reason = reason,
            ErtGroupDetail = ertGroupDetails
        };

        component.CalledErtGroups.Add(ertGroupEnt);

        string message;

        if (component.AutomaticApprove)
        {
            message = Loc.GetString("ert-call-auto-request-announcement",
                ("name", Loc.GetString($"ert-group-full-name-{ertGroupDetails.Name}")),
                ("reviewTime", (int) component.ReviewTime / 60));
        }
        else
        {
            message = Loc.GetString("ert-call-manual-request-announcement", ("name", Loc.GetString($"ert-group-full-name-{ertGroupDetails.Name}")));
        }

        _chatSystem.DispatchGlobalAnnouncement(message, playSound: true,
             colorOverride: Color.Gold);

        RaiseLocalEvent(new RefreshCallErtConsoleEvent(stationUid));
    }


    public bool ReallErt(EntityUid stationUid, int indexGroup, MetaDataComponent? dataComponent = null,
        StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null)
        {
            return false;
        }

        if (component.CalledErtGroups.TryGetValue(indexGroup, out var callErtGroupEnt))
        {
            if (callErtGroupEnt.Status == ErtGroupStatus.Waiting)
            {
                callErtGroupEnt.Status = ErtGroupStatus.Revoke;
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-recall-announcement",
                        ("name", Loc.GetString($"ert-group-full-name-{callErtGroupEnt.ErtGroupDetail!.Name}"))), playSound: true,
                    colorOverride: Color.Gold);
            }
        }

        RaiseLocalEvent(new RefreshCallErtConsoleEvent(stationUid));
        return true;
    }

    public bool ReallApproveErt(EntityUid stationUid, int indexGroup, MetaDataComponent? dataComponent = null,
        StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null)
        {
            return false;
        }

        if (component.CalledErtGroups.TryGetValue(indexGroup, out var callErtGroupEnt))
        {
            if (callErtGroupEnt.Status == ErtGroupStatus.Approved)
            {
                callErtGroupEnt.Status = ErtGroupStatus.Revoke;
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-central-command-recall-announcement",
                        ("name", Loc.GetString($"ert-group-full-name-{callErtGroupEnt.ErtGroupDetail!.Name}"))), playSound: true,
                    colorOverride: Color.Gold);
            }
        }

        RaiseLocalEvent(new RefreshCallErtConsoleEvent(stationUid));
        return true;
    }


    public bool ApproveErt(EntityUid stationUid, int indexGroup, MetaDataComponent? dataComponent = null,
        StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null)
        {
            return false;
        }

        if (component.CalledErtGroups.TryGetValue(indexGroup, out var callErtGroupEnt))
        {
            if (callErtGroupEnt.Status == ErtGroupStatus.Waiting)
            {
                var curTime = _gameTiming.CurTime;
                var waitingTime = callErtGroupEnt.ErtGroupDetail!.WaitingTime;
                callErtGroupEnt.ArrivalTime = curTime + TimeSpan.FromSeconds(waitingTime);
                callErtGroupEnt.Status = ErtGroupStatus.Approved;
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-manual-accepted-announcement",
                        ("name", Loc.GetString($"ert-group-full-name-{callErtGroupEnt.ErtGroupDetail!.Name}")),
                        ("preparationTime", (int)waitingTime/60)), playSound: true,
                    colorOverride: Color.Gold);
            }
        }

        RaiseLocalEvent(new RefreshCallErtConsoleEvent(stationUid));
        return true;
    }


    public bool ToggleAutomateApprove(EntityUid stationUid, bool automateApprove, MetaDataComponent? dataComponent = null,
        StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null)
        {
            return false;
        }

        component.AutomaticApprove = automateApprove;

        RaiseLocalEvent(new RefreshCallErtConsoleEvent(stationUid));
        return true;
    }


    public bool DenyErt(EntityUid stationUid, int indexGroup, MetaDataComponent? dataComponent = null,
        StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null)
        {
            return false;
        }

        if (component.CalledErtGroups.TryGetValue(indexGroup, out var callErtGroupEnt))
        {
            if (callErtGroupEnt.Status == ErtGroupStatus.Waiting)
            {
                callErtGroupEnt.Status = ErtGroupStatus.Denied;
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-manual-refusal-announcement",
                        ("name", Loc.GetString($"ert-group-full-name-{callErtGroupEnt.ErtGroupDetail!.Name}"))), playSound: true,
                    colorOverride: Color.Gold);
            }
        }

        RaiseLocalEvent(new RefreshCallErtConsoleEvent(stationUid));
        return true;
    }


    private bool SpawnErt(ErtGroupDetail? ertGroupDetails, StationCallErtComponent component)
    {
        if (ertGroupDetails == null)
            return false;

        if (component.ErtGroups == null)
            return false;

        if (!TryAddShuttle(ertGroupDetails.ShuttlePath, out var shuttleGrid))
            return false;
        var spawns = new List<EntityCoordinates>();

        foreach (var (_, xform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>(true))
        {
            if (xform.ParentUid != shuttleGrid)
                continue;

            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>(shuttleGrid.Value).Coordinates);
        }

        foreach (var human in ertGroupDetails.HumansList)
        {
            for (var i = 0; i < human.Value; i++)
            {
                EntityManager.SpawnEntity(human.Key, _random.Pick(spawns));
            }
        }

        if (ertGroupDetails.Announcement)
        {
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-spawn-announcement",
                    ("name", Loc.GetString($"ert-group-name-{ertGroupDetails.Name}"))), playSound: true,
                colorOverride: Color.Gold);
        }

        return true;
    }

    public void SendErt(EntityUid stationUid, string ertGroup, MetaDataComponent? dataComponent = null, StationCallErtComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ErtGroups == null
            || !component.ErtGroups.ErtGroupList.TryGetValue(ertGroup, out var ertGroupDetails))
            return;

        var curTime = _gameTiming.CurTime;

        var ertGroupEnt = new CallErtGroupEnt
        {
            Id = ertGroup,
            CalledTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan),
            Status = ErtGroupStatus.Approved,
            ArrivalTime = curTime + TimeSpan.FromSeconds(ertGroupDetails.WaitingTime),
            ReviewTime = TimeSpan.Zero,
            Reason = Loc.GetString("ert-call-central-command-send-groups-cause"),
            ErtGroupDetail = ertGroupDetails
        };

        component.CalledErtGroups.Add(ertGroupEnt);

        string message;

        message = Loc.GetString("ert-call-central-command-send-group-announcement",
            ("name", Loc.GetString($"ert-group-full-name-{ertGroupDetails.Name}")),
            ("preparationTime", (int) component.ReviewTime / 60));

        _chatSystem.DispatchGlobalAnnouncement(message, playSound: true,
            colorOverride: Color.Gold);

        RaiseLocalEvent(new RefreshCallErtConsoleEvent(stationUid));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _gameTiming.CurTime;

         var stationCallErtQuery = EntityQueryEnumerator<StationCallErtComponent>();
         while (stationCallErtQuery.MoveNext(out var stationUid, out var comp))
         {
             if (_gameTiming.CurTime < comp.NextCheck)
                 continue;

             comp.NextCheck = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.CheckInterval);

             var refreshConsoles = false;

             foreach (var calledErtGroup in comp.CalledErtGroups)
             {
                 if (calledErtGroup.Status == ErtGroupStatus.Waiting &&
                     comp.AutomaticApprove &&
                     curTime > calledErtGroup.ReviewTime)
                 {
                     if (CheckCallErt(stationUid, calledErtGroup, comp))
                     {
                         var waitingTime = calledErtGroup.ErtGroupDetail!.WaitingTime;
                         calledErtGroup.ArrivalTime = curTime + TimeSpan.FromSeconds(waitingTime);
                         calledErtGroup.Status = ErtGroupStatus.Approved;
                         refreshConsoles = true;
                         var message = Loc.GetString("ert-call-auto-accepted-announcement",
                             ("name", Loc.GetString($"ert-group-full-name-{calledErtGroup.ErtGroupDetail!.Name}")),
                             ("preparationTime", (int)waitingTime/60));
                         _chatSystem.DispatchGlobalAnnouncement(message, playSound: true,
                             colorOverride: Color.Gold);
                     }
                     else
                     {
                         var message = Loc.GetString("ert-call-auto-refusal-announcement", ("name", Loc.GetString($"ert-group-full-name-{calledErtGroup.ErtGroupDetail!.Name}")));
                         _chatSystem.DispatchGlobalAnnouncement(message, playSound: true,
                             colorOverride: Color.Gold);
                         calledErtGroup.Status = ErtGroupStatus.Denied;
                         refreshConsoles = true;
                     }
                 }

                 if (calledErtGroup.Status == ErtGroupStatus.Approved)
                 {
                     if (curTime < calledErtGroup.ArrivalTime)
                         continue;

                     if (!SpawnErt(calledErtGroup.ErtGroupDetail, comp))
                         continue;

                     calledErtGroup.Status = ErtGroupStatus.Arrived;
                     refreshConsoles = true;
                 }
             }

             if (refreshConsoles)
                RaiseLocalEvent(new RefreshCallErtConsoleEvent(stationUid));
         }
    }
}


public sealed class RefreshCallErtConsoleEvent : EntityEventArgs
{
    public EntityUid Station { get; }

    public RefreshCallErtConsoleEvent(EntityUid station)
    {
        Station = station;
    }
}
