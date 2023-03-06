﻿using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Screens;

[GenerateTypedNameReferences]
public sealed partial class DefaultGameScreen : InGameScreen
{
    public DefaultGameScreen()
    {
        RobustXamlLoader.Load(this);

        AutoscaleMaxResolution = new Vector2i(1080, 770);

        SetAnchorPreset(MainViewport, LayoutPreset.Wide);
        SetAnchorPreset(ViewportContainer, LayoutPreset.Wide);
        SetAnchorAndMarginPreset(TopBar, LayoutPreset.TopLeft, margin: 10);
        SetAnchorAndMarginPreset(Actions, LayoutPreset.BottomLeft, margin: 10);
        SetAnchorAndMarginPreset(Ghost, LayoutPreset.BottomWide, margin: 80);
        SetAnchorAndMarginPreset(Hotbar, LayoutPreset.BottomWide, margin: 5);
        SetAnchorAndMarginPreset(Chat, LayoutPreset.TopRight, margin: 10);
        SetAnchorAndMarginPreset(Alerts, LayoutPreset.TopRight, margin: 10);

        Chat.OnResized += ChatOnResized;
        Chat.OnChatResizeFinish += ChatOnResizeFinish;
    }

    private void ChatOnResizeFinish(Vector2 _)
    {
        var marginBottom = Chat.GetValue<float>(MarginBottomProperty);
        var marginLeft = Chat.GetValue<float>(MarginLeftProperty);
        OnChatResized?.Invoke(new Vector2(marginBottom, marginLeft));
    }

    private void ChatOnResized()
    {
        var marginBottom = Chat.GetValue<float>(MarginBottomProperty);
        SetMarginTop(Alerts, marginBottom);
    }

    public override ChatBox ChatBox => Chat;

    //TODO: There's probably a better way to do this... but this is also the easiest way.
    public override void SetChatSize(Vector2 size)
    {
        SetMarginBottom(Chat, size.X);
        SetMarginLeft(Chat, size.Y);
        SetMarginTop(Alerts, Size.X);
    }
}
