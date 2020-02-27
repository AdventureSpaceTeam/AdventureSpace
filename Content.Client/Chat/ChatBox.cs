﻿using System.Collections.Generic;
using Content.Shared.Chat;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Chat
{
    public class ChatBox : MarginContainer
    {
        public delegate void TextSubmitHandler(ChatBox chatBox, string text);

        public delegate void FilterToggledHandler(ChatBox chatBox, BaseButton.ButtonToggledEventArgs e);

        private readonly ILocalizationManager _localize = IoCManager.Resolve<ILocalizationManager>();

        public HistoryLineEdit Input { get; private set; }
        public OutputPanel Contents { get; }

        // Buttons for filtering
        public Button AllButton { get; }
        public Button LocalButton { get; }
        public Button OOCButton { get; }

        /// <summary>
        ///     Default formatting string for the ClientChatConsole.
        /// </summary>
        public string DefaultChatFormat { get; set; }

        public bool ReleaseFocusOnEnter { get; set; } = true;

        public ChatBox()
        {
            /*MarginLeft = -475.0f;
            MarginTop = 10.0f;
            MarginRight = -10.0f;
            MarginBottom = 235.0f;

            AnchorLeft = 1.0f;
            AnchorRight = 1.0f;*/
            MouseFilter = MouseFilterMode.Stop;

            var outerVBox = new VBoxContainer();

            var panelContainer = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#25252aaa")},
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            var vBox = new VBoxContainer();
            panelContainer.AddChild(vBox);
            var hBox = new HBoxContainer();

            outerVBox.AddChild(panelContainer);
            outerVBox.AddChild(hBox);

            var contentMargin = new MarginContainer
            {
                MarginLeftOverride = 4, MarginRightOverride = 4,
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            Contents = new OutputPanel();
            contentMargin.AddChild(Contents);
            vBox.AddChild(contentMargin);

            Input = new HistoryLineEdit();
            Input.OnKeyBindDown += InputKeyBindDown;
            Input.OnTextEntered += Input_OnTextEntered;
            vBox.AddChild(Input);

            AllButton = new Button
            {
                Text = _localize.GetString("All"),
                Name = "ALL",
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Expand,
                ToggleMode = true,
            };

            LocalButton = new Button
            {
                Text = _localize.GetString("Local"),
                Name = "Local",
                ToggleMode = true,
            };

            OOCButton = new Button
            {
                Text = _localize.GetString("OOC"),
                Name = "OOC",
                ToggleMode = true,
            };

            AllButton.OnToggled += OnFilterToggled;
            LocalButton.OnToggled += OnFilterToggled;
            OOCButton.OnToggled += OnFilterToggled;

            hBox.AddChild(AllButton);
            hBox.AddChild(LocalButton);
            hBox.AddChild(OOCButton);

            AddChild(outerVBox);
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (!args.CanFocus)
            {
                return;
            }

            Input.GrabKeyboardFocus();
        }

        private void InputKeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.TextReleaseFocus)
            {
                Input.ReleaseKeyboardFocus();
                args.Handle();
                return;
            }
        }

        public event TextSubmitHandler TextSubmitted;

        public event FilterToggledHandler FilterToggled;

        public void AddLine(string message, ChatChannel channel, Color color)
        {
            if (Disposed)
            {
                return;
            }

            var formatted = new FormattedMessage(3);
            formatted.PushColor(color);
            formatted.AddText(message);
            formatted.Pop();
            Contents.AddMessage(formatted);
        }

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                TextSubmitted?.Invoke(this, args.Text);
            }

            Input.Clear();

            if (ReleaseFocusOnEnter)
            {
                Input.ReleaseKeyboardFocus();
            }
        }

        private void OnFilterToggled(BaseButton.ButtonToggledEventArgs args)
        {
            FilterToggled?.Invoke(this, args);
        }
    }
}
