using Content.Client._Alteros.CallErt;
using Content.Shared.CallErt;
using JetBrains.Annotations;

namespace Content.Client.CallErt;

[UsedImplicitly]
public sealed class ApproveErtConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ApproveErtConsoleMenu? _menu;

    [ViewVariables]
    private int? SelectedStation { get; set; }

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
        _menu.OpenCentered();
        SendMessage(new CallErtConsoleUpdateMessage());
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

    public void StationSelected(int stationUid)
    {
        SelectedStation = stationUid;
        SendMessage(new CallErtConsoleSelectStationMessage(stationUid));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ApproveErtConsoleInterfaceState approvedErtState)
            return;

        if (_menu == null)
            return;

        _menu.UpdateCalledErtList(approvedErtState.CalledErtsList);
        _menu.UpdateAutomaticApprove(approvedErtState.AutomaticApprove);
        _menu.UpdateStationList(approvedErtState.StationList, approvedErtState.SelectedStation);
        _menu.UpdateStationTime();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
