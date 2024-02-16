﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using FancyWindow = Content.Client.UserInterface.Controls.FancyWindow;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;

namespace Content.Client.Kitchen.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class MicrowaveMenu : FancyWindow
    {
        public sealed class MicrowaveCookTimeButton : Button
        {
            public uint CookTime;
        }

        public event Action<BaseButton.ButtonEventArgs, int>? OnCookTimeSelected;

        public ButtonGroup CookTimeButtonGroup { get; }
        private readonly MicrowaveBoundUserInterface _owner;

        public MicrowaveMenu(MicrowaveBoundUserInterface owner)
        {
            RobustXamlLoader.Load(this);
            CookTimeButtonGroup = new ButtonGroup();
            InstantCookButton.Group = CookTimeButtonGroup;
            _owner = owner;
            InstantCookButton.OnPressed += args =>
            {
                OnCookTimeSelected?.Invoke(args, 0);
            };

            for (var i = 1; i <= 6; i++)
            {
                var newButton = new MicrowaveCookTimeButton
                {
                    Text = (i * 5).ToString(),
                    TextAlign = Label.AlignMode.Center,
                    ToggleMode = true,
                    CookTime = (uint) (i * 5),
                    Group = CookTimeButtonGroup,
                    HorizontalExpand = true,
                };
                if (i == 4)
                {
                    newButton.StyleClasses.Add("OpenRight");
                }
                else
                {
                    newButton.StyleClasses.Add("OpenBoth");
                }
                CookTimeButtonVbox.AddChild(newButton);
                newButton.OnPressed += args =>
                {
                    OnCookTimeSelected?.Invoke(args, i);
                };
            }
        }

        public void ToggleBusyDisableOverlayPanel(bool shouldDisable)
        {
            DisableCookingPanelOverlay.Visible = shouldDisable;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if(!_owner.currentState.IsMicrowaveBusy)
                return;

            if(_owner.currentState.CurrentCookTimeEnd > _owner.GetCurrentTime())
            {
                CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                ("time",_owner.currentState.CurrentCookTimeEnd.Subtract(_owner.GetCurrentTime()).Seconds)); 
            }
        }
    }
}
