using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Robust.Client.AutoGenerated;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Options.UI.Tabs
{
    [GenerateTypedNameReferences]
    public sealed partial class KeyRebindTab : Control
    {
        // List of key functions that must be registered as toggle instead.
        private static readonly HashSet<BoundKeyFunction> ToggleFunctions = new()
        {
            EngineKeyFunctions.ShowDebugMonitors,
            EngineKeyFunctions.HideUI,
        };

        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private BindButton? _currentlyRebinding;

        private readonly Dictionary<BoundKeyFunction, KeyControl> _keyControls =
            new();

        private readonly List<Action> _deferCommands = new();

        private void HandleToggleUSQWERTYCheckbox(BaseButton.ButtonToggledEventArgs args)
        {
            _cfg.SetCVar(CVars.DisplayUSQWERTYHotkeys, args.Pressed);
            _cfg.SaveToFile();
        }

        private void InitToggleWalk()
        {
            if (_cfg.GetCVar(CCVars.ToggleWalk))
            {
                ToggleFunctions.Add(EngineKeyFunctions.Walk);
            }
            else
            {
                ToggleFunctions.Remove(EngineKeyFunctions.Walk);
            }
        }

        private void HandleToggleWalk(BaseButton.ButtonToggledEventArgs args)
        {
            _cfg.SetCVar(CCVars.ToggleWalk, args.Pressed);
            _cfg.SaveToFile();
            InitToggleWalk();

            if (!_keyControls.TryGetValue(EngineKeyFunctions.Walk, out var keyControl))
            {
                return;
            }

            var bindingType = args.Pressed ? KeyBindingType.Toggle : KeyBindingType.State;
            for (var i = 0; i <= 1; i++)
            {
                var binding = (i == 0 ? keyControl.BindButton1 : keyControl.BindButton2).Binding;
                if (binding == null)
                {
                    continue;
                }

                var registration = new KeyBindingRegistration
                {
                    Function = EngineKeyFunctions.Walk,
                    BaseKey = binding.BaseKey,
                    Mod1 = binding.Mod1,
                    Mod2 = binding.Mod2,
                    Mod3 = binding.Mod3,
                    Priority = binding.Priority,
                    Type = bindingType,
                    CanFocus = binding.CanFocus,
                    CanRepeat = binding.CanRepeat,
                };

                _deferCommands.Add(() =>
                {
                    _inputManager.RemoveBinding(binding);
                    _inputManager.RegisterBinding(registration);
                });
            }

            _deferCommands.Add(_inputManager.SaveToUserData);
        }

        private void HandleStaticStorageUI(BaseButton.ButtonToggledEventArgs args)
        {
            _cfg.SetCVar(CCVars.StaticStorageUI, args.Pressed);
            _cfg.SaveToFile();
        }

        public KeyRebindTab()
        {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            ResetAllButton.OnPressed += _ =>
            {
                _deferCommands.Add(() =>
                {
                    _inputManager.ResetAllBindings();
                    _inputManager.SaveToUserData();
                });
            };

            var first = true;

            void AddHeader(string headerContents)
            {
                if (!first)
                {
                    KeybindsContainer.AddChild(new Control { MinSize = new Vector2(0, 8) });
                }

                first = false;
                KeybindsContainer.AddChild(new Label
                {
                    Text = Loc.GetString(headerContents),
                    FontColorOverride = StyleNano.NanoGold,
                    StyleClasses = { StyleNano.StyleClassLabelKeyText }
                });
            }

            void AddButton(BoundKeyFunction function)
            {
                var control = new KeyControl(this, function);
                KeybindsContainer.AddChild(control);
                _keyControls.Add(function, control);
            }

            void AddCheckBox(string checkBoxName, bool currentState, Action<BaseButton.ButtonToggledEventArgs>? callBackOnClick)
            {
                CheckBox newCheckBox = new CheckBox() { Text = Loc.GetString(checkBoxName) };
                newCheckBox.Pressed = currentState;
                newCheckBox.OnToggled += callBackOnClick;

                KeybindsContainer.AddChild(newCheckBox);
            }

            AddHeader("ui-options-header-general");
            AddCheckBox("ui-options-hotkey-keymap", _cfg.GetCVar(CVars.DisplayUSQWERTYHotkeys), HandleToggleUSQWERTYCheckbox);

            AddHeader("ui-options-header-movement");
            AddButton(EngineKeyFunctions.MoveUp);
            AddButton(EngineKeyFunctions.MoveLeft);
            AddButton(EngineKeyFunctions.MoveDown);
            AddButton(EngineKeyFunctions.MoveRight);
            AddButton(EngineKeyFunctions.Walk);
            AddCheckBox("ui-options-hotkey-toggle-walk", _cfg.GetCVar(CCVars.ToggleWalk), HandleToggleWalk);
            InitToggleWalk();

            AddHeader("ui-options-header-camera");
            AddButton(EngineKeyFunctions.CameraRotateLeft);
            AddButton(EngineKeyFunctions.CameraRotateRight);
            AddButton(EngineKeyFunctions.CameraReset);
            AddButton(ContentKeyFunctions.ZoomIn);
            AddButton(ContentKeyFunctions.ZoomOut);
            AddButton(ContentKeyFunctions.ResetZoom);

            AddHeader("ui-options-header-interaction-basic");
            AddButton(EngineKeyFunctions.Use);
            AddButton(EngineKeyFunctions.UseSecondary);
            AddButton(ContentKeyFunctions.UseItemInHand);
            AddButton(ContentKeyFunctions.AltUseItemInHand);
            AddButton(ContentKeyFunctions.ActivateItemInWorld);
            AddButton(ContentKeyFunctions.AltActivateItemInWorld);
            AddButton(ContentKeyFunctions.Drop);
            AddButton(ContentKeyFunctions.ExamineEntity);
            AddButton(ContentKeyFunctions.SwapHands);
            AddButton(ContentKeyFunctions.MoveStoredItem);
            AddButton(ContentKeyFunctions.RotateStoredItem);
            AddButton(ContentKeyFunctions.SaveItemLocation);

            AddHeader("ui-options-header-interaction-adv");
            AddButton(ContentKeyFunctions.SmartEquipBackpack);
            AddButton(ContentKeyFunctions.SmartEquipBelt);
            AddButton(ContentKeyFunctions.OpenBackpack);
            AddButton(ContentKeyFunctions.OpenBelt);
            AddButton(ContentKeyFunctions.ThrowItemInHand);
            AddButton(ContentKeyFunctions.TryPullObject);
            AddButton(ContentKeyFunctions.MovePulledObject);
            AddButton(ContentKeyFunctions.ReleasePulledObject);
            AddButton(ContentKeyFunctions.Point);

            AddHeader("ui-options-header-ui");
            AddButton(ContentKeyFunctions.FocusChat);
            AddButton(ContentKeyFunctions.FocusLocalChat);
            AddButton(ContentKeyFunctions.FocusEmote);
            AddButton(ContentKeyFunctions.FocusWhisperChat);
            AddButton(ContentKeyFunctions.FocusRadio);
            AddButton(ContentKeyFunctions.FocusLOOC);
            AddButton(ContentKeyFunctions.FocusOOC);
            AddButton(ContentKeyFunctions.FocusAdminChat);
            AddButton(ContentKeyFunctions.FocusDeadChat);
            AddButton(ContentKeyFunctions.FocusConsoleChat);
            AddButton(ContentKeyFunctions.CycleChatChannelForward);
            AddButton(ContentKeyFunctions.CycleChatChannelBackward);
            AddButton(ContentKeyFunctions.OpenCharacterMenu);
            AddButton(ContentKeyFunctions.OpenCraftingMenu);
            AddButton(ContentKeyFunctions.OpenGuidebook);
            AddButton(ContentKeyFunctions.OpenInventoryMenu);
            AddButton(ContentKeyFunctions.OpenAHelp);
            AddButton(ContentKeyFunctions.OpenActionsMenu);
            AddButton(ContentKeyFunctions.OpenEntitySpawnWindow);
            AddButton(ContentKeyFunctions.OpenSandboxWindow);
            AddButton(ContentKeyFunctions.OpenTileSpawnWindow);
            AddButton(ContentKeyFunctions.OpenDecalSpawnWindow);
            AddButton(ContentKeyFunctions.OpenAdminMenu);
            AddButton(EngineKeyFunctions.WindowCloseAll);
            AddButton(EngineKeyFunctions.WindowCloseRecent);
            AddButton(EngineKeyFunctions.EscapeMenu);
            AddButton(ContentKeyFunctions.EscapeContext);

            AddHeader("ui-options-header-misc");
            AddButton(ContentKeyFunctions.TakeScreenshot);
            AddButton(ContentKeyFunctions.TakeScreenshotNoUI);
            AddButton(ContentKeyFunctions.ToggleFullscreen);

            AddHeader("ui-options-header-hotbar");
            foreach (var boundKey in ContentKeyFunctions.GetHotbarBoundKeys())
            {
                AddButton(boundKey);
            }

            AddHeader("ui-options-header-shuttle");
            AddButton(ContentKeyFunctions.ShuttleStrafeUp);
            AddButton(ContentKeyFunctions.ShuttleStrafeRight);
            AddButton(ContentKeyFunctions.ShuttleStrafeLeft);
            AddButton(ContentKeyFunctions.ShuttleStrafeDown);
            AddButton(ContentKeyFunctions.ShuttleRotateLeft);
            AddButton(ContentKeyFunctions.ShuttleRotateRight);
            AddButton(ContentKeyFunctions.ShuttleBrake);

            AddHeader("ui-options-header-map-editor");
            AddButton(EngineKeyFunctions.EditorPlaceObject);
            AddButton(EngineKeyFunctions.EditorCancelPlace);
            AddButton(EngineKeyFunctions.EditorGridPlace);
            AddButton(EngineKeyFunctions.EditorLinePlace);
            AddButton(EngineKeyFunctions.EditorRotateObject);
            AddButton(ContentKeyFunctions.EditorFlipObject);
            AddButton(ContentKeyFunctions.EditorCopyObject);

            AddHeader("ui-options-header-dev");
            AddButton(EngineKeyFunctions.ShowDebugConsole);
            AddButton(EngineKeyFunctions.ShowDebugMonitors);
            AddButton(EngineKeyFunctions.HideUI);
            AddButton(ContentKeyFunctions.InspectEntity);

            foreach (var control in _keyControls.Values)
            {
                UpdateKeyControl(control);
            }
        }

        private void UpdateKeyControl(KeyControl control)
        {
            var activeBinds = _inputManager.GetKeyBindings(control.Function);

            IKeyBinding? bind1 = null;
            IKeyBinding? bind2 = null;

            if (activeBinds.Count > 0)
            {
                bind1 = activeBinds[0];

                if (activeBinds.Count > 1)
                {
                    bind2 = activeBinds[1];
                }
            }

            control.BindButton1.Binding = bind1;
            control.BindButton1.UpdateText();

            control.BindButton2.Binding = bind2;
            control.BindButton2.UpdateText();

            control.BindButton2.Button.Disabled = activeBinds.Count == 0;
            control.ResetButton.Disabled = !_inputManager.IsKeyFunctionModified(control.Function);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _inputManager.FirstChanceOnKeyEvent += InputManagerOnFirstChanceOnKeyEvent;
            _inputManager.OnKeyBindingAdded += OnKeyBindAdded;
            _inputManager.OnKeyBindingRemoved += OnKeyBindRemoved;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            _inputManager.FirstChanceOnKeyEvent -= InputManagerOnFirstChanceOnKeyEvent;
            _inputManager.OnKeyBindingAdded -= OnKeyBindAdded;
            _inputManager.OnKeyBindingRemoved -= OnKeyBindRemoved;
        }

        private void OnKeyBindRemoved(IKeyBinding obj)
        {
            OnKeyBindModified(obj, true);
        }

        private void OnKeyBindAdded(IKeyBinding obj)
        {
            OnKeyBindModified(obj, false);
        }

        private void OnKeyBindModified(IKeyBinding bind, bool removal)
        {
            if (!_keyControls.TryGetValue(bind.Function, out var keyControl))
            {
                return;
            }

            if (removal && _currentlyRebinding?.KeyControl == keyControl)
            {
                // Don't do update if the removal was from initiating a rebind.
                return;
            }

            UpdateKeyControl(keyControl);

            if (_currentlyRebinding == keyControl.BindButton1 || _currentlyRebinding == keyControl.BindButton2)
            {
                _currentlyRebinding = null;
            }
        }

        private void InputManagerOnFirstChanceOnKeyEvent(KeyEventArgs keyEvent, KeyEventType type)
        {
            DebugTools.Assert(IsInsideTree);

            if (_currentlyRebinding == null)
            {
                return;
            }

            keyEvent.Handle();

            if (type != KeyEventType.Up)
            {
                return;
            }

            var key = keyEvent.Key;

            // Figure out modifiers based on key event.
            // TODO: this won't allow for combinations with keys other than the standard modifier keys,
            // even though the input system totally supports it.
            var mods = new Keyboard.Key[3];
            var i = 0;
            if (keyEvent.Control && key != Keyboard.Key.Control)
            {
                mods[i] = Keyboard.Key.Control;
                i += 1;
            }

            if (keyEvent.Shift && key != Keyboard.Key.Shift)
            {
                mods[i] = Keyboard.Key.Shift;
                i += 1;
            }

            if (keyEvent.Alt && key != Keyboard.Key.Alt)
            {
                mods[i] = Keyboard.Key.Alt;
                i += 1;
            }

            // The input system can only handle 3 modifier keys so if you hold all 4 of the modifier keys
            // then system gets the shaft, I guess.
            if (keyEvent.System && i != 3 && key != Keyboard.Key.LSystem && key != Keyboard.Key.RSystem)
            {
                mods[i] = Keyboard.Key.LSystem;
            }

            var function = _currentlyRebinding.KeyControl.Function;
            var bindType = KeyBindingType.State;
            if (ToggleFunctions.Contains(function))
            {
                bindType = KeyBindingType.Toggle;
            }

            var registration = new KeyBindingRegistration
            {
                Function = function,
                BaseKey = key,
                Mod1 = mods[0],
                Mod2 = mods[1],
                Mod3 = mods[2],
                Priority = _currentlyRebinding.Binding?.Priority ?? 0,
                Type = bindType,
                CanFocus = key == Keyboard.Key.MouseLeft
                           || key == Keyboard.Key.MouseRight
                           || key == Keyboard.Key.MouseMiddle,
                CanRepeat = false
            };

            _inputManager.RegisterBinding(registration);
            // OnKeyBindModified will cause _currentlyRebinding to be reset and the UI to update.
            _inputManager.SaveToUserData();
        }

        private void RebindButtonPressed(BindButton button)
        {
            if (_currentlyRebinding != null)
            {
                return;
            }

            _currentlyRebinding = button;
            _currentlyRebinding.Button.Text = Loc.GetString("ui-options-key-prompt");

            if (button.Binding != null)
            {
                _deferCommands.Add(() =>
                {
                    // Have to do defer this or else there will be an exception in InputManager.
                    // Because this IS fired from an input event.
                    _inputManager.RemoveBinding(button.Binding);
                });
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_deferCommands.Count == 0)
            {
                return;
            }

            foreach (var command in _deferCommands)
            {
                command();
            }

            _deferCommands.Clear();
        }

        private sealed class KeyControl : Control
        {
            public readonly BoundKeyFunction Function;
            public readonly BindButton BindButton1;
            public readonly BindButton BindButton2;
            public readonly Button ResetButton;

            public KeyControl(KeyRebindTab parent, BoundKeyFunction function)
            {
                Function = function;
                var name = new Label
                {
                    Text = Loc.GetString(
                        $"ui-options-function-{CaseConversion.PascalToKebab(function.FunctionName)}"),
                    HorizontalExpand = true,
                    HorizontalAlignment = HAlignment.Left
                };

                BindButton1 = new BindButton(parent, this, StyleBase.ButtonOpenRight);
                BindButton2 = new BindButton(parent, this, StyleBase.ButtonOpenLeft);
                ResetButton = new Button { Text = Loc.GetString("ui-options-bind-reset"), StyleClasses = { StyleBase.ButtonCaution } };

                var hBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Control {MinSize = new Vector2(5, 0)},
                        name,
                        BindButton1,
                        BindButton2,
                        new Control {MinSize = new Vector2(10, 0)},
                        ResetButton
                    }
                };

                ResetButton.OnPressed += args =>
                {
                    parent._deferCommands.Add(() =>
                    {
                        parent._inputManager.ResetBindingsFor(function);
                        parent._inputManager.SaveToUserData();
                    });
                };

                AddChild(hBox);
            }
        }

        private sealed class BindButton : Control
        {
            private readonly KeyRebindTab _tab;
            public readonly KeyControl KeyControl;
            public readonly Button Button;
            public IKeyBinding? Binding;

            public BindButton(KeyRebindTab tab, KeyControl keyControl, string styleClass)
            {
                _tab = tab;
                KeyControl = keyControl;
                Button = new Button { StyleClasses = { styleClass } };
                UpdateText();
                AddChild(Button);

                Button.OnPressed += args =>
                {
                    tab.RebindButtonPressed(this);
                };

                Button.OnKeyBindDown += ButtonOnOnKeyBindDown;

                MinSize = new Vector2(200, 0);
            }

            protected override void EnteredTree()
            {
                base.EnteredTree();
                _tab._inputManager.OnInputModeChanged += UpdateText;
            }

            protected override void ExitedTree()
            {
                base.ExitedTree();
                _tab._inputManager.OnInputModeChanged -= UpdateText;
            }

            private void ButtonOnOnKeyBindDown(GUIBoundKeyEventArgs args)
            {
                if (args.Function == EngineKeyFunctions.UIRightClick)
                {
                    if (Binding != null)
                    {
                        _tab._deferCommands.Add(() =>
                        {
                            _tab._inputManager.RemoveBinding(Binding);
                            _tab._inputManager.SaveToUserData();
                        });
                    }

                    args.Handle();
                }
            }

            public void UpdateText()
            {
                Button.Text = Binding?.GetKeyString() ?? Loc.GetString("ui-options-unbound");
            }
        }
    }
}
