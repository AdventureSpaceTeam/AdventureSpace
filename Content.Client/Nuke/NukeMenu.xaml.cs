using System;
using Content.Shared.Nuke;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;

namespace Content.Client.Nuke
{
    [GenerateTypedNameReferences]
    public partial class NukeMenu : SS14Window
    {
        public event Action<int>? OnKeypadButtonPressed;
        public event Action? OnClearButtonPressed;
        public event Action? OnEnterButtonPressed;

        public NukeMenu()
        {
            RobustXamlLoader.Load(this);
            FillKeypadGrid();
        }

        /// <summary>
        ///     Fill keypad buttons in keypad grid
        /// </summary>
        private void FillKeypadGrid()
        {
            // add 3 rows of keypad buttons (1-9)
            for (var i = 1; i <= 9; i++)
            {
                AddKeypadButton(i);
            }

            // clear button
            var clearBtn = new Button()
            {
                Text = "C"
            };
            clearBtn.OnPressed += _ => OnClearButtonPressed?.Invoke();
            KeypadGrid.AddChild(clearBtn);

            // zero button
            AddKeypadButton(0);

            // enter button
            var enterBtn = new Button()
            {
                Text = "E"
            };
            enterBtn.OnPressed += _ => OnEnterButtonPressed?.Invoke();
            KeypadGrid.AddChild(enterBtn);
        }

        private void AddKeypadButton(int i)
        {
            var btn = new Button()
            {
                Text = i.ToString()
            };

            btn.OnPressed += _ => OnKeypadButtonPressed?.Invoke(i);
            KeypadGrid.AddChild(btn);
        }

        public void UpdateState(NukeUiState state)
        {
            string firstMsg, secondMsg;
            switch (state.Status)
            {
                case NukeStatus.AWAIT_DISK:
                    firstMsg = Loc.GetString("nuke-user-interface-first-status-device-locked");
                    secondMsg = Loc.GetString("nuke-user-interface-second-status-await-disk");
                    break;
                case NukeStatus.AWAIT_CODE:
                    firstMsg = Loc.GetString("nuke-user-interface-first-status-input-code");
                    secondMsg = Loc.GetString("nuke-user-interface-second-status-current-code",
                        ("code", VisualizeCode(state.EnteredCodeLength, state.MaxCodeLength)));
                    break;
                case NukeStatus.AWAIT_ARM:
                    firstMsg = Loc.GetString("nuke-user-interface-first-status-device-ready");
                    secondMsg = Loc.GetString("nuke-user-interface-second-status-time",
                        ("time", state.RemainingTime));
                    break;
                case NukeStatus.ARMED:
                    firstMsg = Loc.GetString("nuke-user-interface-first-status-device-armed");
                    secondMsg = Loc.GetString("nuke-user-interface-second-status-time",
                        ("time", state.RemainingTime));
                    break;
                default:
                    // shouldn't normally be here
                    firstMsg = Loc.GetString("nuke-user-interface-status-error");
                    secondMsg = Loc.GetString("nuke-user-interface-status-error");
                    break;
            }

            FirstStatusLabel.Text = firstMsg;
            SecondStatusLabel.Text = secondMsg;

            EjectButton.Disabled = !state.DiskInserted;
            AnchorButton.Disabled = !state.DiskInserted;
            AnchorButton.Pressed = state.IsAnchored;
            ArmButton.Disabled = !state.AllowArm;
        }

        private string VisualizeCode(int codeLength, int maxLength)
        {
            var code = new string('*', codeLength);
            var blanksCount = maxLength - codeLength;
            var blanks = new string('_', blanksCount);
            return code + blanks;
        }
    }
}
