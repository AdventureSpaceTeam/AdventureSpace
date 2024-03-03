using Content.Client._Alteros.CallErt;
using System.Linq;
using Content.Shared.CallErt;
using JetBrains.Annotations;

namespace Content.Client.CallErt;

[UsedImplicitly]
public sealed class ApproveErtConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ApproveErtConsoleMenu? _menu;

    [ViewVariables]
    public bool CanSendErt { get; private set; }

    public ApproveErtConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new ApproveErtConsoleMenu(this);
        _menu.OnClose += Close;
        _menu.ApproveErt += ApproveErt;
        _menu.DenyErt += DenyErt;
        _menu.RecallErt += RecallErt;
        _menu.OpenCentered();
        SendMessage(new CallErtConsoleUpdateMessage());
    }


    private void RecallErt(int indexGroup)
    {
        var station = (NetEntity?) _menu?.StationSelector.SelectedMetadata;

        if (station == null)
            return;

        SendMessage(new ApproveErtConsoleRecallErtMessage(station.Value, indexGroup));
    }

    private void ApproveErt(int indexGroup)
    {
        SendMessage(new CallErtConsoleApproveErtMessage(indexGroup));
    }

    public void EnableAutomateApprove(bool automateApprove)
    {
        SendMessage(new CallErtConsoleToggleAutomateApproveErtMessage(automateApprove));
    }

    private void DenyErt(int indexGroup)
    {
        SendMessage(new CallErtConsoleDenyErtMessage(indexGroup));
    }

    public void StationSelected(NetEntity stationUid)
    {
        SendMessage(new CallErtConsoleSelectStationMessage(stationUid));
    }

    public void ErtGroupSelected(string group)
    {
        SendMessage(new CallErtConsoleSelectErtMessage(group));
    }

    public void SendErtButtonPressed()
    {
        SendErt();
    }

    private void SendErt()
    {
        var station = (NetEntity?) _menu?.StationSelector.SelectedMetadata;
        var ertGroup = (string?) _menu?.ErtGroupSelector.SelectedMetadata;

        if (station == null || ertGroup == null)
            return;

        SendMessage(new CallErtConsoleSendErtMessage(station.Value, ertGroup));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ApproveErtConsoleInterfaceState approvedErtState)
            return;

        CanSendErt = approvedErtState.CanSendErt;

        if (_menu == null)
            return;

        _menu.UpdateErtList(approvedErtState.ErtsList, approvedErtState.SelectedErtGroup);
        _menu.UpdateCalledErtList(approvedErtState.CalledErtsList);
        _menu.UpdateAutomaticApprove(approvedErtState.AutomaticApprove);
        _menu.UpdateStationList(approvedErtState.StationList, approvedErtState.SelectedStation);
        _menu.UpdateStationTime();
        _menu.SendErt.Disabled = !CanSendErt;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
