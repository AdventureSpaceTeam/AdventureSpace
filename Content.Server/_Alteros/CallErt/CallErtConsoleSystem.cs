using System.Linq;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CallErt;
using Content.Shared.Emag.Components;
using Robust.Server.GameObjects;

namespace Content.Server.CallErt;

public sealed class CallErtConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly StationCallErtSystem _stationCallErtSystem = default!;

    private const float UIUpdateInterval = 5.0f;

    public override void Initialize()
    {
        SubscribeLocalEvent<RefreshCallErtConsoleEvent>(OnRefreshCallErtConsole);
        SubscribeLocalEvent<CallErtConsoleComponent, CallErtConsoleCallErtMessage>(OnCallErtMessage);
        SubscribeLocalEvent<CallErtConsoleComponent, CallErtConsoleRecallErtMessage>(OnRecallErtMessage);
        SubscribeLocalEvent<CallErtConsoleComponent, CallErtConsoleUpdateMessage>(OnUpdateCallErtConsoleMessage);
        SubscribeLocalEvent<CallErtConsoleComponent, CallErtConsoleSelectErtMessage>(OnSelectErtMessage);
        SubscribeLocalEvent<ApproveErtConsoleComponent, ApproveErtConsoleRecallErtMessage>(OnRecallApproveErtMessage);
        SubscribeLocalEvent<ApproveErtConsoleComponent, CallErtConsoleSendErtMessage>(OnSendErtMessage);
        SubscribeLocalEvent<ApproveErtConsoleComponent, CallErtConsoleSelectErtMessage>(OnSelectErtMessage);
        SubscribeLocalEvent<ApproveErtConsoleComponent, CallErtConsoleApproveErtMessage>(OnApproveErtMessage);
        SubscribeLocalEvent<ApproveErtConsoleComponent, CallErtConsoleDenyErtMessage>(OnDenyErtMessage);
        SubscribeLocalEvent<ApproveErtConsoleComponent, CallErtConsoleToggleAutomateApproveErtMessage>(OnToggleAutomateApproveErtMessage);
        SubscribeLocalEvent<ApproveErtConsoleComponent, CallErtConsoleUpdateMessage>(OnUpdateApproveErtConsoleMessage);
        SubscribeLocalEvent<ApproveErtConsoleComponent, CallErtConsoleSelectStationMessage>(OnSelectStatioMessage);
    }

    private void OnSelectStatioMessage(EntityUid uid, ApproveErtConsoleComponent comp,
        CallErtConsoleSelectStationMessage message)
    {
        if (message.Session.AttachedEntity is not {Valid: true} mob)
            return;

        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
            return;
        }

        comp.SelectedStation = GetEntity(message.StationUid);
        comp.SelectedErtGroup = null;

        UpdateApproveConsoleInterface(uid, comp);
    }

    private void OnToggleAutomateApproveErtMessage(EntityUid uid, ApproveErtConsoleComponent comp,
        CallErtConsoleToggleAutomateApproveErtMessage message)
    {
        if (message.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
            return;
        }
        if (comp.SelectedStation == null)
            return;

        if (!_stationCallErtSystem.ToggleAutomateApprove(comp.SelectedStation.Value, message.AutomateApprove))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-call-ert-fall"), uid, message.Session);
            return;
        }

        UpdateApproveConsoleInterface(uid, comp);
    }


    private void OnApproveErtMessage(EntityUid uid, ApproveErtConsoleComponent comp,
        CallErtConsoleApproveErtMessage message)
    {
        if (message.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
            return;
        }

        if (comp.SelectedStation == null)
            return;

        if (!_stationCallErtSystem.ApproveErt(comp.SelectedStation.Value, message.IndexGroup))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-call-ert-fall"), uid, message.Session);
            return;
        }

        UpdateApproveConsoleInterface(uid, comp);
    }

    private void OnDenyErtMessage(EntityUid uid, ApproveErtConsoleComponent comp,
        CallErtConsoleDenyErtMessage message)
    {
        if (message.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
            return;
        }

        if (comp.SelectedStation == null)
            return;

        if (!_stationCallErtSystem.DenyErt(comp.SelectedStation.Value, message.IndexGroup))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-call-ert-fall"), uid, message.Session);
            return;
        }

        UpdateApproveConsoleInterface(uid, comp);
    }

    private void OnUpdateCallErtConsoleMessage(EntityUid uid, CallErtConsoleComponent comp,
                                             CallErtConsoleUpdateMessage message)
    {
        if (_ui.TryGetUi(uid, CallErtConsoleUiKey.Key, out var ui) && ui.SubscribedSessions.Count > 0)
            UpdateCallConsoleInterface(uid, comp, ui);
    }

    private void OnUpdateApproveErtConsoleMessage(EntityUid uid, ApproveErtConsoleComponent comp,
                                                CallErtConsoleUpdateMessage message)
    {
        if (_ui.TryGetUi(uid, ApproveErtConsoleUiKey.Key, out var ui) && ui.SubscribedSessions.Count > 0)
            UpdateApproveConsoleInterface(uid, comp, ui);
    }

    public override void Update(float frameTime)
    {
        var queryCallErtConsole = EntityQueryEnumerator<CallErtConsoleComponent>();
        while (queryCallErtConsole.MoveNext(out var uid, out var comp))
        {
            // TODO refresh the UI in a less horrible way
            if (comp.CallErtCooldownRemaining >= 0f)
            {
                comp.CallErtCooldownRemaining -= frameTime;
            }

            comp.UIUpdateAccumulator += frameTime;

            if (comp.UIUpdateAccumulator < UIUpdateInterval)
                continue;

            comp.UIUpdateAccumulator -= UIUpdateInterval;

            if (_ui.TryGetUi(uid, CallErtConsoleUiKey.Key, out var ui) && ui.SubscribedSessions.Count > 0)
                UpdateCallConsoleInterface(uid, comp, ui);
        }

        var queryApproveErtConsole = EntityQueryEnumerator<ApproveErtConsoleComponent>();
        while (queryApproveErtConsole.MoveNext(out var uid, out var comp))
        {
            // TODO refresh the UI in a less horrible way
            if (comp.SendErtCooldownRemaining >= 0f)
            {
                comp.SendErtCooldownRemaining -= frameTime;
            }

            comp.UIUpdateAccumulator += frameTime;

            if (comp.UIUpdateAccumulator < UIUpdateInterval)
                continue;

            comp.UIUpdateAccumulator -= UIUpdateInterval;

            if (_ui.TryGetUi(uid, ApproveErtConsoleUiKey.Key, out var ui) && ui.SubscribedSessions.Count > 0)
                UpdateApproveConsoleInterface(uid, comp, ui);
        }

        base.Update(frameTime);
    }

    private void OnCallErtMessage(EntityUid uid, CallErtConsoleComponent comp, CallErtConsoleCallErtMessage message)
    {
        if (message.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
            return;
        }

        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null || !CanCallOrRecallErt(comp))
            return;

        _stationCallErtSystem.CallErt(stationUid.Value, message.ErtGroup, message.Reason);
        comp.CallErtCooldownRemaining = comp.DelayBetweenCallErt;
        UpdateCallConsoleInterface(uid, comp);
    }

    private bool CanUse(EntityUid user, EntityUid console)
    {
        // This shouldn't technically be possible because of BUI but don't trust client.
        if (!_interaction.InRangeUnobstructed(console, user))
            return false;

        if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent) && !HasComp<EmaggedComponent>(console))
        {
            return _accessReaderSystem.IsAllowed(user, console, accessReaderComponent);
        }
        return true;
    }

    private void OnRecallErtMessage(EntityUid uid, CallErtConsoleComponent comp,
        CallErtConsoleRecallErtMessage message)
    {
        if (message.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
            return;
        }

        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid == null)
            return;

        if (!_stationCallErtSystem.ReallErt(stationUid.Value, message.IndexGroup))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-call-ert-fall"), uid, message.Session);
            return;
        }

        UpdateCallConsoleInterface(uid, comp);
    }

    private void OnSelectErtMessage(EntityUid uid, CallErtConsoleComponent comp,
        CallErtConsoleSelectErtMessage message)
    {
        comp.SelectedErtGroup = message.ErtGroup;
        UpdateCallConsoleInterface(uid, comp);
    }

    private void UpdateCallConsoleInterface(EntityUid uid, CallErtConsoleComponent? component = null, PlayerBoundUserInterface? ui = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (ui == null && !_ui.TryGetUi(uid, CallErtConsoleUiKey.Key, out ui))
            return;

        var stationUid = _stationSystem.GetOwningStation(uid);
        var ertsList = new Dictionary<string, ErtGroupDetail>();
        var calledErtsList = new List<CallErtGroupEnt>();

        if (stationUid != null)
        {
            if (TryComp<StationCallErtComponent>(stationUid.Value, out var ertComponent) && ertComponent.ErtGroups != null)
            {
                foreach (var (id, detail) in ertComponent.ErtGroups.ErtGroupList)
                {
                    if (detail.ShowInConsole)
                        ertsList.Add(id, detail);
                }
                foreach (var detail in ertComponent.CalledErtGroups)
                {
                    calledErtsList.Add(detail);
                }
            }
        }

        var state = new CallErtConsoleInterfaceState(CanCallOrRecallErt(component), ertsList, calledErtsList, component.SelectedErtGroup);
        _ui.SetUiState(ui, state);
    }

    private void OnRefreshCallErtConsole(RefreshCallErtConsoleEvent args)
    {
        var queryCallErtConsole = EntityQueryEnumerator<CallErtConsoleComponent>();
        while (queryCallErtConsole.MoveNext(out var uid, out var comp))
        {
            var entStation = _stationSystem.GetOwningStation(uid);
            if (args.Station == entStation)
                UpdateCallConsoleInterface(uid, comp);
        }

        var queryApproveErtConsole = EntityQueryEnumerator<ApproveErtConsoleComponent>();
        while (queryApproveErtConsole.MoveNext(out var uid, out var comp))
        {
            var entStation = _stationSystem.GetOwningStation(uid);
            if (args.Station == entStation)
                UpdateApproveConsoleInterface(uid, comp);
        }
    }

    private bool CanCallOrRecallErt(CallErtConsoleComponent comp)
    {
        return comp.CallErtCooldownRemaining <= 0f;
    }

    private bool CanSendErt(ApproveErtConsoleComponent comp)
    {
        return comp.SendErtCooldownRemaining <= 0f;
    }

    private void UpdateApproveConsoleInterface(EntityUid uid, ApproveErtConsoleComponent? component = null, PlayerBoundUserInterface? ui = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (ui == null && !_ui.TryGetUi(uid, ApproveErtConsoleUiKey.Key, out ui))
            return;

        var automaticApprove = false;

        var stations = _stationSystem.GetStations();

        var stationsList = new Dictionary<NetEntity, string>();
        foreach (var entityUid in stations)
        {
            if (HasComp<StationCallErtComponent>(entityUid))
                stationsList.Add(GetNetEntity(entityUid), MetaData(entityUid).EntityName);
        }

        if (stationsList.Count > 0 && component.SelectedStation == null)
            component.SelectedStation = stations.First();

        var calledErtsList = new List<CallErtGroupEnt>();
        var ertsList = new Dictionary<string, ErtGroupDetail>();
        if (component.SelectedStation != null)
        {
            if (TryComp<StationCallErtComponent>(component.SelectedStation, out var ertComponent) && ertComponent.ErtGroups != null)
            {
                automaticApprove = ertComponent.AutomaticApprove;
                foreach (var detail in ertComponent.CalledErtGroups)
                {
                    calledErtsList.Add(detail);
                }

                foreach (var (id, detail) in ertComponent.ErtGroups.ErtGroupList)
                {
                    ertsList.Add(id, detail);
                }
            }
        }

        var state = new ApproveErtConsoleInterfaceState(CanSendErt(component), automaticApprove, ertsList, calledErtsList, stationsList, GetNetEntity(component.SelectedStation), component.SelectedErtGroup);
        _ui.SetUiState(ui, state);
    }

    private void OnSendErtMessage(EntityUid uid, ApproveErtConsoleComponent comp, CallErtConsoleSendErtMessage message)
    {
        if (message.Session.AttachedEntity is not {Valid: true} mob)
            return;

        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
            return;
        }

        var stationUid = GetEntity(message.Station);
        if (!CanSendErt(comp))
            return;

        _stationCallErtSystem.SendErt(stationUid, message.ErtGroup);
        comp.SendErtCooldownRemaining = comp.DelayBetweenSendErt;
        UpdateApproveConsoleInterface(uid, comp);
    }



    private void OnRecallApproveErtMessage(EntityUid uid, ApproveErtConsoleComponent comp,
        ApproveErtConsoleRecallErtMessage message)
    {
        if (message.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Session);
            return;
        }

        var stationUid = GetEntity(message.Station);

        if (!_stationCallErtSystem.ReallApproveErt(stationUid, message.IndexGroup))
        {
            _popupSystem.PopupEntity(Loc.GetString("comms-console-call-ert-fall"), uid, message.Session);
            return;
        }

        UpdateApproveConsoleInterface(uid, comp);
    }

    private void OnSelectErtMessage(EntityUid uid, ApproveErtConsoleComponent comp,
        CallErtConsoleSelectErtMessage message)
    {
        comp.SelectedErtGroup = message.ErtGroup;
        UpdateApproveConsoleInterface(uid, comp);
    }
}
