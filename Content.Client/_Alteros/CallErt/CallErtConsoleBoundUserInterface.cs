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
    public bool CanCallErt { get; private set; }

    private readonly int maxReasonLength = 256;

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
        var ertGroup = (string?) _menu?.ErtGroupSelector.SelectedMetadata;

        if (ertGroup == null || _menu == null)
            return;

        var stringContent = Rope.Collapse(_menu.ReasonInput.TextRope);
        var content = (stringContent.Length <= maxReasonLength ? stringContent.Trim() : $"{stringContent.Trim().Substring(0, maxReasonLength)}...");
        SendMessage(new CallErtConsoleCallErtMessage(ertGroup, content));
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
        SendMessage(new CallErtConsoleSelectErtMessage(group));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CallErtConsoleInterfaceState callErtState)
            return;

        CanCallErt = callErtState.CanCallErt;

        if (_menu == null)
            return;

        _menu.UpdateErtList(callErtState.ErtsList, callErtState.SelectedErtGroup);
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
