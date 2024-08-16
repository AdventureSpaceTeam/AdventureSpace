using Content.Shared.Body.Part;
using Robust.Client.UserInterface;

namespace Content.Client._c4llv07e.Bridges;

public interface ITargetDollWidgetBridge
{
    public void SelectBodyPart(BodyPartType? targetBodyPart, BodyPartSymmetry bodyPartSymmetry);
    public void Clear();
    public void Hide();
    public void Show();
    public void InitializeWidget();
    public void SetupWidget(Control surface);
}

public sealed class StubTargetDollWidgetBridge : ITargetDollWidgetBridge
{
    public void SelectBodyPart(BodyPartType? targetBodyPart, BodyPartSymmetry bodyPartSymmetry)
    {
    }

    public void Clear()
    {

    }

    public void Hide()
    {
    }

    public void Show()
    {
    }

    public void InitializeWidget()
    {
    }

    public void SetupWidget(Control surface)
    {

    }
}
