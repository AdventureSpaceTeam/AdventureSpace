using System.Linq;
using Content.Client._Alteros.CallErt;
using Content.Shared.CallErt;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Client.CallErt;

[UsedImplicitly]
public sealed class CallErtConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CallErtConsoleMenu? _menu;
    [ViewVariables]
    private string? SelectedErtGroup { get; set; }
    [ViewVariables]
    public bool CanCallErt { get; private set; }

    private int maxReasonLength = 256;

    public CallErtConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new CallErtConsoleMenu(this);
        _menu.OnClose += Close;
        _menu.RecallErt += RecallErt;
        _menu.OpenCentered();

        SendMessage(new CallErtConsoleUpdateMessage());
    }

    private void CallErt()
    {
        if (_menu == null || SelectedErtGroup == null)
            return;

        var stringContent = Rope.Collapse(_menu.ReasonInput.TextRope);
        var content = (stringContent.Length <= maxReasonLength ? stringContent.Trim() : $"{stringContent.Trim().Substring(0, maxReasonLength)}...");
        SendMessage(new CallErtConsoleCallErtMessage(SelectedErtGroup, content));
    }

    private void RecallErt(int indexGroup)
    {
        SendMessage(new CallErtConsoleRecallErtMessage(indexGroup));
    }

    public void CallErtButtonPressed()
    {
        CallErt();
    }

    public void ErtGroupSelected(string group)
    {
        SelectedErtGroup = group;
        SendMessage(new CallErtConsoleSelectErtMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CallErtConsoleInterfaceState callErtState)
            return;

        CanCallErt = callErtState.CanCallErt;

        if (string.IsNullOrEmpty(SelectedErtGroup))
        {
            if (callErtState.ErtsList is { Count: > 0 })
            {
                SelectedErtGroup = callErtState.ErtsList.First().Key;
            }
        }

        if (_menu == null)
            return;

        _menu.UpdateErtList(callErtState.ErtsList, SelectedErtGroup);
        _menu.UpdateCalledErtList(callErtState.CalledErtsList);
        _menu.UpdateStationTime();
        _menu.CallErt.Disabled = !CanCallErt;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
