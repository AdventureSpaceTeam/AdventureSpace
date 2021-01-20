﻿using System;
using System.Transactions;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Input;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.Core.Tokens;
using static Robust.Client.Input.Keyboard.Key;
using Control = Robust.Client.UserInterface.Control;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     Responsible for laying out the default game HUD.
    /// </summary>
    public interface IGameHud
    {
        Control RootControl { get; }

        // Escape top button.
        bool EscapeButtonDown { get; set; }
        Action<bool> EscapeButtonToggled { get; set; }

        // Character top button.
        bool CharacterButtonDown { get; set; }
        bool CharacterButtonVisible { get; set; }
        Action<bool> CharacterButtonToggled { get; set; }

        // Inventory top button.
        bool InventoryButtonDown { get; set; }
        bool InventoryButtonVisible { get; set; }
        Action<bool> InventoryButtonToggled { get; set; }

        // Crafting top button.
        bool CraftingButtonDown { get; set; }
        bool CraftingButtonVisible { get; set; }
        Action<bool> CraftingButtonToggled { get; set; }

        // Actions top button.
        bool ActionsButtonDown { get; set; }
        bool ActionsButtonVisible { get; set; }
        Action<bool> ActionsButtonToggled { get; set; }

        // Admin top button.
        bool AdminButtonDown { get; set; }
        bool AdminButtonVisible { get; set; }
        Action<bool> AdminButtonToggled { get; set; }

        // Sandbox top button.
        bool SandboxButtonDown { get; set; }
        bool SandboxButtonVisible { get; set; }
        Action<bool> SandboxButtonToggled { get; set; }

        Control HandsContainer { get; }
        Control SuspicionContainer { get; }
        Control RightInventoryQuickButtonContainer { get; }
        Control LeftInventoryQuickButtonContainer { get; }

        bool CombatPanelVisible { get; set; }
        bool CombatModeActive { get; set; }
        TargetingZone TargetingZone { get; set; }
        Action<bool> OnCombatModeChanged { get; set; }
        Action<TargetingZone> OnTargetingZoneChanged { get; set; }


        // Init logic.
        void Initialize();
    }

    internal sealed class GameHud : IGameHud
    {
        private HBoxContainer _topButtonsContainer;
        private TopButton _buttonEscapeMenu;
        private TopButton _buttonTutorial;
        private TopButton _buttonCharacterMenu;
        private TopButton _buttonInventoryMenu;
        private TopButton _buttonCraftingMenu;
        private TopButton _buttonActionsMenu;
        private TopButton _buttonAdminMenu;
        private TopButton _buttonSandboxMenu;
        private TutorialWindow _tutorialWindow;
        private TargetingDoll _targetingDoll;
        private Button _combatModeButton;
        private VBoxContainer _combatPanelContainer;

        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        public Control HandsContainer { get; private set; }
        public Control SuspicionContainer { get; private set; }
        public Control RightInventoryQuickButtonContainer { get; private set; }
        public Control LeftInventoryQuickButtonContainer { get; private set; }

        public bool CombatPanelVisible
        {
            get => _combatPanelContainer.Visible;
            set => _combatPanelContainer.Visible = value;
        }

        public bool CombatModeActive
        {
            get => _combatModeButton.Pressed;
            set => _combatModeButton.Pressed = value;
        }

        public TargetingZone TargetingZone
        {
            get => _targetingDoll.ActiveZone;
            set => _targetingDoll.ActiveZone = value;
        }

        public Action<bool> OnCombatModeChanged { get; set; }
        public Action<TargetingZone> OnTargetingZoneChanged { get; set; }

        public void Initialize()
        {
            RootControl = new LayoutContainer();
            LayoutContainer.SetAnchorPreset(RootControl, LayoutContainer.LayoutPreset.Wide);

            var escapeTexture = _resourceCache.GetTexture("/Textures/Interface/hamburger.svg.192dpi.png");
            var characterTexture = _resourceCache.GetTexture("/Textures/Interface/character.svg.192dpi.png");
            var inventoryTexture = _resourceCache.GetTexture("/Textures/Interface/inventory.svg.192dpi.png");
            var craftingTexture = _resourceCache.GetTexture("/Textures/Interface/hammer.svg.192dpi.png");
            var actionsTexture = _resourceCache.GetTexture("/Textures/Interface/fist.svg.192dpi.png");
            var adminTexture = _resourceCache.GetTexture("/Textures/Interface/gavel.svg.192dpi.png");
            var tutorialTexture = _resourceCache.GetTexture("/Textures/Interface/tutorial.svg.192dpi.png");
            var sandboxTexture = _resourceCache.GetTexture("/Textures/Interface/sandbox.svg.192dpi.png");

            _topButtonsContainer = new HBoxContainer
            {
                SeparationOverride = 8
            };

            RootControl.AddChild(_topButtonsContainer);

            LayoutContainer.SetAnchorAndMarginPreset(_topButtonsContainer, LayoutContainer.LayoutPreset.TopLeft,
                margin: 10);

            // the icon textures here should all have the same image height (32) but different widths, so in order to ensure
            // the buttons themselves are consistent widths we set a common custom min size
            Vector2 topMinSize = (42, 64);

            // Escape
            _buttonEscapeMenu = new TopButton(escapeTexture, EngineKeyFunctions.EscapeMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open escape menu."),
                CustomMinimumSize = (70, 64),
                StyleClasses = {StyleBase.ButtonOpenRight}
            };

            _topButtonsContainer.AddChild(_buttonEscapeMenu);

            _buttonEscapeMenu.OnToggled += args => EscapeButtonToggled?.Invoke(args.Pressed);

            // Character
            _buttonCharacterMenu = new TopButton(characterTexture, ContentKeyFunctions.OpenCharacterMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open character menu."),
                CustomMinimumSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonCharacterMenu);

            _buttonCharacterMenu.OnToggled += args => CharacterButtonToggled?.Invoke(args.Pressed);

            // Inventory
            _buttonInventoryMenu = new TopButton(inventoryTexture, ContentKeyFunctions.OpenInventoryMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open inventory menu."),
                CustomMinimumSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonInventoryMenu);

            _buttonInventoryMenu.OnToggled += args => InventoryButtonToggled?.Invoke(args.Pressed);

            // Crafting
            _buttonCraftingMenu = new TopButton(craftingTexture, ContentKeyFunctions.OpenCraftingMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open crafting menu."),
                CustomMinimumSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonCraftingMenu);

            _buttonCraftingMenu.OnToggled += args => CraftingButtonToggled?.Invoke(args.Pressed);

            // Actions
            _buttonActionsMenu = new TopButton(actionsTexture, ContentKeyFunctions.OpenActionsMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open actions menu."),
                CustomMinimumSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonActionsMenu);

            _buttonActionsMenu.OnToggled += args => ActionsButtonToggled?.Invoke(args.Pressed);

            // Admin
            _buttonAdminMenu = new TopButton(adminTexture, ContentKeyFunctions.OpenAdminMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open admin menu."),
                CustomMinimumSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonAdminMenu);

            _buttonAdminMenu.OnToggled += args => AdminButtonToggled?.Invoke(args.Pressed);

            // Sandbox
            _buttonSandboxMenu = new TopButton(sandboxTexture, ContentKeyFunctions.OpenSandboxWindow, _inputManager)
            {
                ToolTip = Loc.GetString("Open sandbox menu."),
                CustomMinimumSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonSandboxMenu);

            _buttonSandboxMenu.OnToggled += args => SandboxButtonToggled?.Invoke(args.Pressed);

            // Tutorial
            _buttonTutorial = new TopButton(tutorialTexture, ContentKeyFunctions.OpenTutorial, _inputManager)
            {
                ToolTip = Loc.GetString("Open tutorial."),
                CustomMinimumSize = topMinSize,
                StyleClasses = {StyleBase.ButtonOpenLeft, TopButton.StyleClassRedTopButton},
            };

            _topButtonsContainer.AddChild(_buttonTutorial);

            _buttonTutorial.OnToggled += a => ButtonTutorialOnOnToggled();

            _tutorialWindow = new TutorialWindow();

            _tutorialWindow.OnClose += () => _buttonTutorial.Pressed = false;

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenTutorial,
                InputCmdHandler.FromDelegate(s => ButtonTutorialOnOnToggled()));


            _combatPanelContainer = new VBoxContainer
            {
                Children =
                {
                    (_combatModeButton = new Button
                    {
                        Text = Loc.GetString("Combat Mode"),
                        ToggleMode = true
                    }),
                    (_targetingDoll = new TargetingDoll(_resourceCache))
                }
            };

            LayoutContainer.SetGrowHorizontal(_combatPanelContainer, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetGrowVertical(_combatPanelContainer, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetAnchorAndMarginPreset(_combatPanelContainer, LayoutContainer.LayoutPreset.BottomRight);
            LayoutContainer.SetMarginBottom(_combatPanelContainer, -10f);
            RootControl.AddChild(_combatPanelContainer);

            _combatModeButton.OnToggled += args => OnCombatModeChanged?.Invoke(args.Pressed);
            _targetingDoll.OnZoneChanged += args => OnTargetingZoneChanged?.Invoke(args);

            var centerBottomContainer = new HBoxContainer
            {
                SeparationOverride = 5
            };
            LayoutContainer.SetAnchorAndMarginPreset(centerBottomContainer, LayoutContainer.LayoutPreset.CenterBottom);
            LayoutContainer.SetGrowHorizontal(centerBottomContainer, LayoutContainer.GrowDirection.Both);
            LayoutContainer.SetGrowVertical(centerBottomContainer, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetMarginBottom(centerBottomContainer, -10f);
            RootControl.AddChild(centerBottomContainer);

            HandsContainer = new MarginContainer
            {
                SizeFlagsVertical = Control.SizeFlags.ShrinkEnd
            };
            RightInventoryQuickButtonContainer = new MarginContainer
            {
                SizeFlagsVertical = Control.SizeFlags.ShrinkEnd
            };
            LeftInventoryQuickButtonContainer = new MarginContainer
            {
                SizeFlagsVertical = Control.SizeFlags.ShrinkEnd
            };
            centerBottomContainer.AddChild(LeftInventoryQuickButtonContainer);
            centerBottomContainer.AddChild(HandsContainer);
            centerBottomContainer.AddChild(RightInventoryQuickButtonContainer);

            SuspicionContainer = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
            };

            RootControl.AddChild(SuspicionContainer);

            LayoutContainer.SetAnchorAndMarginPreset(SuspicionContainer, LayoutContainer.LayoutPreset.BottomLeft,
                margin: 10);
            LayoutContainer.SetGrowHorizontal(SuspicionContainer, LayoutContainer.GrowDirection.End);
            LayoutContainer.SetGrowVertical(SuspicionContainer, LayoutContainer.GrowDirection.Begin);
        }

        private void ButtonTutorialOnOnToggled()
        {
            _buttonTutorial.StyleClasses.Remove(TopButton.StyleClassRedTopButton);
            if (_tutorialWindow.IsOpen)
            {
                if (!_tutorialWindow.IsAtFront())
                {
                    _tutorialWindow.MoveToFront();
                    _buttonTutorial.Pressed = true;
                }
                else
                {
                    _tutorialWindow.Close();
                    _buttonTutorial.Pressed = false;
                }
            }
            else
            {
                _tutorialWindow.OpenCentered();
                _buttonTutorial.Pressed = true;
            }
        }

        public Control RootControl { get; private set; }

        public bool EscapeButtonDown
        {
            get => _buttonEscapeMenu.Pressed;
            set => _buttonEscapeMenu.Pressed = value;
        }

        public Action<bool> EscapeButtonToggled { get; set; }

        public bool CharacterButtonDown
        {
            get => _buttonCharacterMenu.Pressed;
            set => _buttonCharacterMenu.Pressed = value;
        }

        public bool CharacterButtonVisible
        {
            get => _buttonCharacterMenu.Visible;
            set => _buttonCharacterMenu.Visible = value;
        }

        public Action<bool> CharacterButtonToggled { get; set; }

        public bool InventoryButtonDown
        {
            get => _buttonInventoryMenu.Pressed;
            set => _buttonInventoryMenu.Pressed = value;
        }

        public bool InventoryButtonVisible
        {
            get => _buttonInventoryMenu.Visible;
            set => _buttonInventoryMenu.Visible = value;
        }

        public Action<bool> InventoryButtonToggled { get; set; }

        public bool CraftingButtonDown
        {
            get => _buttonCraftingMenu.Pressed;
            set => _buttonCraftingMenu.Pressed = value;
        }

        public bool CraftingButtonVisible
        {
            get => _buttonCraftingMenu.Visible;
            set => _buttonCraftingMenu.Visible = value;
        }

        public Action<bool> CraftingButtonToggled { get; set; }

        public bool ActionsButtonDown
        {
            get => _buttonActionsMenu.Pressed;
            set => _buttonActionsMenu.Pressed = value;
        }

        public bool ActionsButtonVisible
        {
            get => _buttonActionsMenu.Visible;
            set => _buttonActionsMenu.Visible = value;
        }

        public Action<bool> ActionsButtonToggled { get; set; }

        public bool AdminButtonDown
        {
            get => _buttonAdminMenu.Pressed;
            set => _buttonAdminMenu.Pressed = value;
        }

        public bool AdminButtonVisible
        {
            get => _buttonAdminMenu.Visible;
            set => _buttonAdminMenu.Visible = value;
        }

        public Action<bool> AdminButtonToggled { get; set; }

        public bool SandboxButtonDown
        {
            get => _buttonSandboxMenu.Pressed;
            set => _buttonSandboxMenu.Pressed = value;
        }

        public bool SandboxButtonVisible
        {
            get => _buttonSandboxMenu.Visible;
            set => _buttonSandboxMenu.Visible = value;
        }

        public Action<bool> SandboxButtonToggled { get; set; }

        public sealed class TopButton : ContainerButton
        {
            public const string StyleClassLabelTopButton = "topButtonLabel";
            public const string StyleClassRedTopButton = "topButtonLabel";
            private const float CustomTooltipDelay = 0.4f;

            private static readonly Color ColorNormal = Color.FromHex("#7b7e9e");
            private static readonly Color ColorRedNormal = Color.FromHex("#FEFEFE");
            private static readonly Color ColorHovered = Color.FromHex("#9699bb");
            private static readonly Color ColorRedHovered = Color.FromHex("#FFFFFF");
            private static readonly Color ColorPressed = Color.FromHex("#789B8C");

            private const float VertPad = 8f;

            private Color NormalColor => HasStyleClass(StyleClassRedTopButton) ? ColorRedNormal : ColorNormal;
            private Color HoveredColor => HasStyleClass(StyleClassRedTopButton) ? ColorRedHovered : ColorHovered;

            private readonly TextureRect _textureRect;
            private readonly Label _label;
            private readonly BoundKeyFunction _function;
            private readonly IInputManager _inputManager;

            public TopButton(Texture texture, BoundKeyFunction function, IInputManager inputManager)
            {
                _function = function;
                _inputManager = inputManager;
                TooltipDelay = CustomTooltipDelay;

                AddChild(
                    new VBoxContainer
                    {
                        Children =
                        {
                            new Control {CustomMinimumSize = (0, VertPad)},
                            (_textureRect = new TextureRect
                            {
                                TextureScale = (0.5f, 0.5f),
                                Texture = texture,
                                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                SizeFlagsVertical = SizeFlags.Expand | SizeFlags.ShrinkCenter,
                                ModulateSelfOverride = NormalColor,
                                Stretch = TextureRect.StretchMode.KeepCentered
                            }),
                            new Control {CustomMinimumSize = (0, VertPad)},
                            (_label = new Label
                            {
                                Text = ShortKeyName(_function),
                                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                ModulateSelfOverride = NormalColor,
                                StyleClasses = {StyleClassLabelTopButton}
                            })
                        }
                    }
                );

                ToggleMode = true;
            }

            protected override void EnteredTree()
            {
                _inputManager.OnKeyBindingAdded += OnKeyBindingChanged;
                _inputManager.OnKeyBindingRemoved += OnKeyBindingChanged;
            }

            protected override void ExitedTree()
            {
                _inputManager.OnKeyBindingAdded -= OnKeyBindingChanged;
                _inputManager.OnKeyBindingRemoved -= OnKeyBindingChanged;
            }


            private void OnKeyBindingChanged(IKeyBinding obj)
            {
                _label.Text = ShortKeyName(_function);
            }

            private string ShortKeyName(BoundKeyFunction keyFunction)
            {
                // need to use shortened key names so they fit in the buttons.
                return TryGetShortKeyName(keyFunction, out var name) ? Loc.GetString(name) : " ";
            }

            private bool TryGetShortKeyName(BoundKeyFunction keyFunction, out string name)
            {
                if (_inputManager.TryGetKeyBinding(keyFunction, out var binding))
                {
                    // can't possibly fit a modifier key in the top button, so omit it
                    var key = binding.BaseKey;
                    if (binding.Mod1 != Unknown || binding.Mod2 != Unknown ||
                        binding.Mod3 != Unknown)
                    {
                        name = null;
                        return false;
                    }

                    name = null;
                    name = key switch
                    {
                        Apostrophe => "'",
                        Comma => ",",
                        Delete => "Del",
                        Down => "Dwn",
                        Escape => "Esc",
                        Equal => "=",
                        Home => "Hom",
                        Insert => "Ins",
                        Left => "Lft",
                        Menu => "Men",
                        Minus => "-",
                        Num0 => "0",
                        Num1 => "1",
                        Num2 => "2",
                        Num3 => "3",
                        Num4 => "4",
                        Num5 => "5",
                        Num6 => "6",
                        Num7 => "7",
                        Num8 => "8",
                        Num9 => "9",
                        Pause => "||",
                        Period => ".",
                        Return => "Ret",
                        Right => "Rgt",
                        Slash => "/",
                        Space => "Spc",
                        Tab => "Tab",
                        Tilde => "~",
                        BackSlash => "\\",
                        BackSpace => "Bks",
                        LBracket => "[",
                        MouseButton4 => "M4",
                        MouseButton5 => "M5",
                        MouseButton6 => "M6",
                        MouseButton7 => "M7",
                        MouseButton8 => "M8",
                        MouseButton9 => "M9",
                        MouseLeft => "ML",
                        MouseMiddle => "MM",
                        MouseRight => "MR",
                        NumpadDecimal => "N.",
                        NumpadDivide => "N/",
                        NumpadEnter => "Ent",
                        NumpadMultiply => "*",
                        NumpadNum0 => "0",
                        NumpadNum1 => "1",
                        NumpadNum2 => "2",
                        NumpadNum3 => "3",
                        NumpadNum4 => "4",
                        NumpadNum5 => "5",
                        NumpadNum6 => "6",
                        NumpadNum7 => "7",
                        NumpadNum8 => "8",
                        NumpadNum9 => "9",
                        NumpadSubtract => "N-",
                        PageDown => "PgD",
                        PageUp => "PgU",
                        RBracket => "]",
                        SemiColon => ";",
                        _ => DefaultShortKeyName(keyFunction)
                    };
                    return name != null;
                }

                name = null;
                return false;
            }

            private string DefaultShortKeyName(BoundKeyFunction keyFunction)
            {
                var name = FormattedMessage.EscapeText(_inputManager.GetKeyFunctionButtonString(keyFunction));
                return name.Length > 3 ? null : name;
            }

            protected override void StylePropertiesChanged()
            {
                // colors of children depend on style, so ensure we update when style is changed
                base.StylePropertiesChanged();
                UpdateChildColors();
            }

            private void UpdateChildColors()
            {
                if (_label == null || _textureRect == null) return;
                switch (DrawMode)
                {
                    case DrawModeEnum.Normal:
                        _textureRect.ModulateSelfOverride = NormalColor;
                        _label.ModulateSelfOverride = NormalColor;
                        break;

                    case DrawModeEnum.Pressed:
                        _textureRect.ModulateSelfOverride = ColorPressed;
                        _label.ModulateSelfOverride = ColorPressed;
                        break;

                    case DrawModeEnum.Hover:
                        _textureRect.ModulateSelfOverride = HoveredColor;
                        _label.ModulateSelfOverride = HoveredColor;
                        break;

                    case DrawModeEnum.Disabled:
                        break;
                }
            }


            protected override void DrawModeChanged()
            {
                base.DrawModeChanged();
                UpdateChildColors();
            }
        }
    }
}
