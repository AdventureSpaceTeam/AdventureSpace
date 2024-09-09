using System.Numerics;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Screens;

[GenerateTypedNameReferences]
public sealed partial class SeparatedChatGameScreen : InGameScreen
{
    public SeparatedChatGameScreen()
    {
        RobustXamlLoader.Load(this);

        AutoscaleMaxResolution = new Vector2i(1080, 770);

        SetAnchorPreset(ScreenContainer, LayoutPreset.Wide);
        SetAnchorPreset(ViewportContainer, LayoutPreset.Wide);
        SetAnchorPreset(MainViewport, LayoutPreset.Wide);
        SetAnchorAndMarginPreset(Inventory, LayoutPreset.BottomLeft, margin: 5);
        SetAnchorAndMarginPreset(TopLeftContainer, LayoutPreset.TopLeft, margin: 10);
        SetAnchorAndMarginPreset(Ghost, LayoutPreset.BottomWide, margin: 80);
        SetAnchorAndMarginPreset(Hotbar, LayoutPreset.BottomWide, margin: 5);
        SetAnchorAndMarginPreset(Alerts, LayoutPreset.CenterRight, margin: 10);

        ScreenContainer.OnSplitResizeFinished += () =>
            OnChatResized?.Invoke(new Vector2(ScreenContainer.SplitFraction, 0));

        ViewportContainer.OnResized += ResizeActionContainer;
    }

    private void ResizeActionContainer()
    {
        float indent = 20;
        Actions.ActionsContainer.MaxGridWidth = ViewportContainer.Size.X - indent;
    }

    public override ChatBox ChatBox => GetWidget<ChatBox>()!;

    public override void SetChatSize(Vector2 size)
    {
        ScreenContainer.DesiredSplitCenter = size.X;
        ScreenContainer.ResizeMode = SplitContainer.SplitResizeMode.RespectChildrenMinSize;
    }
}
