﻿using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.PDA;

[GenerateTypedNameReferences]
public sealed partial class PDASettingsButton : ContainerButton
{
    public const string StylePropertyFgColor = "foregroundColor";
    public const string StylePropertyBgColor = "backgroundColor";
    public const string NormalBgColor = "#313138";
    public const string HoverColor = "#3E6C45";
    public const string PressedColor = "#3E6C45";
    public const string DisabledFgColor = "#5a5a5a";
    public const string EnabledFgColor = "#FFFFFF";

    private readonly StyleBoxFlat _styleBox = new()
    {
        BackgroundColor = Color.FromHex("#25252a")
    };

    public string? Text
    {
        get => OptionName.Text;
        set => OptionName.Text = value;
    }
    public string? Description
    {
        get => OptionDescription.Text;
        set => OptionDescription.Text = value;
    }

    public Color BackgroundColor
    {
        get => _styleBox.BackgroundColor;
        set => _styleBox.BackgroundColor = value;
    }

    public Color? ForegroundColor
    {
        get => OptionName.FontColorOverride;

        set
        {
            OptionName.FontColorOverride = value;
            OptionDescription.FontColorOverride = value;
        }
    }

    public PDASettingsButton()
    {
        RobustXamlLoader.Load(this);
        Panel.PanelOverride = _styleBox;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (TryGetStyleProperty<Color>(StylePropertyBgColor, out var bgColor))
            BackgroundColor = bgColor;

        if (TryGetStyleProperty<Color>(StylePropertyFgColor, out var fgColor))
            ForegroundColor = fgColor;

    }
}
