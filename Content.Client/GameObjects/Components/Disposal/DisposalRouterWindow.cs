﻿using Content.Shared.GameObjects.Components.Disposal;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalRouterComponent;

namespace Content.Client.GameObjects.Components.Disposal
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedDisposalRouterComponent"/>
    /// </summary>
    public class DisposalRouterWindow : SS14Window
    {
        public readonly LineEdit TagInput;
        public readonly Button Confirm;

        protected override Vector2? CustomSize => (400, 80);

        public DisposalRouterWindow()
        {
            Title = Loc.GetString("Disposal Router");

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new Label {Text = Loc.GetString("Tags:")},
                    new Control {CustomMinimumSize = (0, 10)},
                    new HBoxContainer
                    {
                        Children =
                        {
                            (TagInput = new LineEdit {SizeFlagsHorizontal = SizeFlags.Expand, CustomMinimumSize = (320, 0),
                                ToolTip = Loc.GetString("A comma separated list of tags"), IsValid = tags => TagRegex.IsMatch(tags)}),
                            new Control {CustomMinimumSize = (10, 0)},
                            (Confirm = new Button {Text = Loc.GetString("Confirm")})
                        }
                    }
                }
            });
        }


        public void UpdateState(DisposalRouterUserInterfaceState state)
        {
            TagInput.Text = state.Tags;
        }
    }
}
