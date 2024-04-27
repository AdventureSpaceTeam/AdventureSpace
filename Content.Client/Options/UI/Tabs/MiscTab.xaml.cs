﻿using System.Linq;
using Content.Client.UserInterface.Screens;
using Content.Shared.CCVar;
using Content.Shared.HUD;
using Robust.Client.AutoGenerated;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Range = Robust.Client.UserInterface.Controls.Range;

namespace Content.Client.Options.UI.Tabs
{
    [GenerateTypedNameReferences]
    public sealed partial class MiscTab : Control
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<string, int> _hudThemeIdToIndex = new();

        public MiscTab()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            var themes = _prototypeManager.EnumeratePrototypes<HudThemePrototype>().ToList();
            themes.Sort();
            foreach (var gear in themes)
            {
                HudThemeOption.AddItem(Loc.GetString(gear.Name));
                _hudThemeIdToIndex.Add(gear.ID, HudThemeOption.GetItemId(HudThemeOption.ItemCount - 1));
            }

            var hudLayout = _cfg.GetCVar(CCVars.UILayout);
            var id = 0;
            foreach (var layout in Enum.GetValues(typeof(ScreenType)))
            {
                var name = layout.ToString()!;
                HudLayoutOption.AddItem(name, id);
                if (name == hudLayout)
                {
                    HudLayoutOption.SelectId(id);
                }
                HudLayoutOption.SetItemMetadata(id, name);

                id++;
            }

            HudLayoutOption.OnItemSelected += args =>
            {
                HudLayoutOption.SelectId(args.Id);
                UpdateApplyButton();
            };

            // Channel can be null in replays so.
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            ShowOocPatronColor.Visible = _playerManager.LocalSession?.Channel?.UserData.PatronTier is { };

            HudThemeOption.OnItemSelected += OnHudThemeChanged;
            DiscordRich.OnToggled += OnCheckBoxToggled;
            ShowOocPatronColor.OnToggled += OnCheckBoxToggled;
            ShowLoocAboveHeadCheckBox.OnToggled += OnCheckBoxToggled;
            ShowHeldItemCheckBox.OnToggled += OnCheckBoxToggled;
            ShowCombatModeIndicatorsCheckBox.OnToggled += OnCheckBoxToggled;
            OpaqueStorageWindowCheckBox.OnToggled += OnCheckBoxToggled;
            FancySpeechBubblesCheckBox.OnToggled += OnCheckBoxToggled;
            FancyNameBackgroundsCheckBox.OnToggled += OnCheckBoxToggled;
            EnableColorNameCheckBox.OnToggled += OnCheckBoxToggled;
            ColorblindFriendlyCheckBox.OnToggled += OnCheckBoxToggled;
            ReducedMotionCheckBox.OnToggled += OnCheckBoxToggled;
            ChatWindowOpacitySlider.OnValueChanged += OnChatWindowOpacitySliderChanged;
            ScreenShakeIntensitySlider.OnValueChanged += OnScreenShakeIntensitySliderChanged;
            // ToggleWalk.OnToggled += OnCheckBoxToggled;
            StaticStorageUI.OnToggled += OnCheckBoxToggled;

            HudThemeOption.SelectId(_hudThemeIdToIndex.GetValueOrDefault(_cfg.GetCVar(CVars.InterfaceTheme), 0));
            DiscordRich.Pressed = _cfg.GetCVar(CVars.DiscordEnabled);
            ShowOocPatronColor.Pressed = _cfg.GetCVar(CCVars.ShowOocPatronColor);
            ShowLoocAboveHeadCheckBox.Pressed = _cfg.GetCVar(CCVars.LoocAboveHeadShow);
            ShowHeldItemCheckBox.Pressed = _cfg.GetCVar(CCVars.HudHeldItemShow);
            ShowCombatModeIndicatorsCheckBox.Pressed = _cfg.GetCVar(CCVars.CombatModeIndicatorsPointShow);
            OpaqueStorageWindowCheckBox.Pressed = _cfg.GetCVar(CCVars.OpaqueStorageWindow);
            FancySpeechBubblesCheckBox.Pressed = _cfg.GetCVar(CCVars.ChatEnableFancyBubbles);
            FancyNameBackgroundsCheckBox.Pressed = _cfg.GetCVar(CCVars.ChatFancyNameBackground);
            EnableColorNameCheckBox.Pressed = _cfg.GetCVar(CCVars.ChatEnableColorName);
            ColorblindFriendlyCheckBox.Pressed = _cfg.GetCVar(CCVars.AccessibilityColorblindFriendly);
            ReducedMotionCheckBox.Pressed = _cfg.GetCVar(CCVars.ReducedMotion);
            ChatWindowOpacitySlider.Value = _cfg.GetCVar(CCVars.ChatWindowOpacity);
            ScreenShakeIntensitySlider.Value = _cfg.GetCVar(CCVars.ScreenShakeIntensity) * 100f;
            // ToggleWalk.Pressed = _cfg.GetCVar(CCVars.ToggleWalk);
            StaticStorageUI.Pressed = _cfg.GetCVar(CCVars.StaticStorageUI);


            ApplyButton.OnPressed += OnApplyButtonPressed;
            UpdateApplyButton();
        }

        private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            UpdateApplyButton();
        }

        private void OnHudThemeChanged(OptionButton.ItemSelectedEventArgs args)
        {
            HudThemeOption.SelectId(args.Id);
            UpdateApplyButton();
        }

        private void OnChatWindowOpacitySliderChanged(Range range)
        {
            ChatWindowOpacityLabel.Text = Loc.GetString("ui-options-chat-window-opacity-percent",
                ("opacity", range.Value));
            UpdateApplyButton();
        }

        private void OnScreenShakeIntensitySliderChanged(Range obj)
        {
            ScreenShakeIntensityLabel.Text = Loc.GetString("ui-options-screen-shake-percent", ("intensity", ScreenShakeIntensitySlider.Value / 100f));
            UpdateApplyButton();
        }

        private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
        {
            foreach (var theme in _prototypeManager.EnumeratePrototypes<HudThemePrototype>())
            {
                if (_hudThemeIdToIndex[theme.ID] != HudThemeOption.SelectedId)
                    continue;
                _cfg.SetCVar(CVars.InterfaceTheme, theme.ID);
                break;
            }

            _cfg.SetCVar(CVars.DiscordEnabled, DiscordRich.Pressed);
            _cfg.SetCVar(CCVars.HudHeldItemShow, ShowHeldItemCheckBox.Pressed);
            _cfg.SetCVar(CCVars.CombatModeIndicatorsPointShow, ShowCombatModeIndicatorsCheckBox.Pressed);
            _cfg.SetCVar(CCVars.OpaqueStorageWindow, OpaqueStorageWindowCheckBox.Pressed);
            _cfg.SetCVar(CCVars.ShowOocPatronColor, ShowOocPatronColor.Pressed);
            _cfg.SetCVar(CCVars.LoocAboveHeadShow, ShowLoocAboveHeadCheckBox.Pressed);
            _cfg.SetCVar(CCVars.ChatEnableFancyBubbles, FancySpeechBubblesCheckBox.Pressed);
            _cfg.SetCVar(CCVars.ChatFancyNameBackground, FancyNameBackgroundsCheckBox.Pressed);
            _cfg.SetCVar(CCVars.ChatEnableColorName, EnableColorNameCheckBox.Pressed);
            _cfg.SetCVar(CCVars.AccessibilityColorblindFriendly, ColorblindFriendlyCheckBox.Pressed);
            _cfg.SetCVar(CCVars.ReducedMotion, ReducedMotionCheckBox.Pressed);
            _cfg.SetCVar(CCVars.ChatWindowOpacity, ChatWindowOpacitySlider.Value);
            _cfg.SetCVar(CCVars.ScreenShakeIntensity, ScreenShakeIntensitySlider.Value / 100f);
            // _cfg.SetCVar(CCVars.ToggleWalk, ToggleWalk.Pressed);
            _cfg.SetCVar(CCVars.StaticStorageUI, StaticStorageUI.Pressed);

            if (HudLayoutOption.SelectedMetadata is string opt)
            {
                _cfg.SetCVar(CCVars.UILayout, opt);
            }

            _cfg.SaveToFile();
            UpdateApplyButton();
        }

        private void UpdateApplyButton()
        {
            var isHudThemeSame = HudThemeOption.SelectedId == _hudThemeIdToIndex.GetValueOrDefault(_cfg.GetCVar(CVars.InterfaceTheme), 0);
            var isLayoutSame = HudLayoutOption.SelectedMetadata is string opt && opt == _cfg.GetCVar(CCVars.UILayout);
            var isDiscordSame = DiscordRich.Pressed == _cfg.GetCVar(CVars.DiscordEnabled);
            var isShowHeldItemSame = ShowHeldItemCheckBox.Pressed == _cfg.GetCVar(CCVars.HudHeldItemShow);
            var isCombatModeIndicatorsSame = ShowCombatModeIndicatorsCheckBox.Pressed == _cfg.GetCVar(CCVars.CombatModeIndicatorsPointShow);
            var isOpaqueStorageWindow = OpaqueStorageWindowCheckBox.Pressed == _cfg.GetCVar(CCVars.OpaqueStorageWindow);
            var isOocPatronColorShowSame = ShowOocPatronColor.Pressed == _cfg.GetCVar(CCVars.ShowOocPatronColor);
            var isLoocShowSame = ShowLoocAboveHeadCheckBox.Pressed == _cfg.GetCVar(CCVars.LoocAboveHeadShow);
            var isFancyChatSame = FancySpeechBubblesCheckBox.Pressed == _cfg.GetCVar(CCVars.ChatEnableFancyBubbles);
            var isFancyBackgroundSame = FancyNameBackgroundsCheckBox.Pressed == _cfg.GetCVar(CCVars.ChatFancyNameBackground);
            var isEnableColorNameSame = EnableColorNameCheckBox.Pressed == _cfg.GetCVar(CCVars.ChatEnableColorName);
            var isColorblindFriendly = ColorblindFriendlyCheckBox.Pressed == _cfg.GetCVar(CCVars.AccessibilityColorblindFriendly);
            var isReducedMotionSame = ReducedMotionCheckBox.Pressed == _cfg.GetCVar(CCVars.ReducedMotion);
            var isChatWindowOpacitySame = Math.Abs(ChatWindowOpacitySlider.Value - _cfg.GetCVar(CCVars.ChatWindowOpacity)) < 0.01f;
            var isScreenShakeIntensitySame = Math.Abs(ScreenShakeIntensitySlider.Value / 100f - _cfg.GetCVar(CCVars.ScreenShakeIntensity)) < 0.01f;
            // var isToggleWalkSame = ToggleWalk.Pressed == _cfg.GetCVar(CCVars.ToggleWalk);
            var isStaticStorageUISame = StaticStorageUI.Pressed == _cfg.GetCVar(CCVars.StaticStorageUI);

            ApplyButton.Disabled = isHudThemeSame &&
                                   isLayoutSame &&
                                   isDiscordSame &&
                                   isShowHeldItemSame &&
                                   isCombatModeIndicatorsSame &&
                                   isOpaqueStorageWindow &&
                                   isOocPatronColorShowSame &&
                                   isLoocShowSame &&
                                   isFancyChatSame &&
                                   isFancyBackgroundSame &&
                                   isEnableColorNameSame &&
                                   isColorblindFriendly &&
                                   isReducedMotionSame &&
                                   isChatWindowOpacitySame &&
                                   isScreenShakeIntensitySame &&
                                   // isToggleWalkSame &&
                                   isStaticStorageUISame;
        }

    }

}
