using System.Collections.Generic;
using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.SharedIdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class IdCardConsoleWindow : DefaultWindow
    {
        private readonly IdCardConsoleBoundUserInterface _owner;

        private readonly Dictionary<string, Button> _accessButtons = new();

        private string? _lastFullName;
        private string? _lastJobTitle;

        public IdCardConsoleWindow(IdCardConsoleBoundUserInterface owner, IPrototypeManager prototypeManager, List<string> accessLevels)
        {
            RobustXamlLoader.Load(this);

            _owner = owner;

            FullNameLineEdit.OnTextEntered += _ => SubmitData();
            FullNameLineEdit.OnTextChanged += _ =>
            {
                FullNameSaveButton.Disabled = FullNameSaveButton.Text == _lastFullName;
            };
            FullNameSaveButton.OnPressed += _ => SubmitData();

            JobTitleLineEdit.OnTextEntered += _ => SubmitData();
            JobTitleLineEdit.OnTextChanged += _ =>
            {
                JobTitleSaveButton.Disabled = JobTitleLineEdit.Text == _lastJobTitle;
            };
            JobTitleSaveButton.OnPressed += _ => SubmitData();

            foreach (var access in accessLevels)
            {
                if (!prototypeManager.TryIndex<AccessLevelPrototype>(access, out var accessLevel))
                {
                    Logger.ErrorS(SharedIdCardConsoleSystem.Sawmill, $"Unable to find accesslevel for {access}");
                    continue;
                }

                var newButton = new Button
                {
                    Text = accessLevel.Name,
                    ToggleMode = true,
                };
                AccessLevelGrid.AddChild(newButton);
                _accessButtons.Add(accessLevel.ID, newButton);
                newButton.OnPressed += _ => SubmitData();
            }
        }

        public void UpdateState(IdCardConsoleBoundUserInterfaceState state)
        {
            PrivilegedIdButton.Text = state.IsPrivilegedIdPresent
                ? Loc.GetString("id-card-console-window-eject-button")
                : Loc.GetString("id-card-console-window-insert-button");

            PrivilegedIdLabel.Text = state.PrivilegedIdName;

            TargetIdButton.Text = state.IsTargetIdPresent
                ? Loc.GetString("id-card-console-window-eject-button")
                : Loc.GetString("id-card-console-window-insert-button");

            TargetIdLabel.Text = state.TargetIdName;

            var interfaceEnabled =
                state.IsPrivilegedIdPresent && state.IsPrivilegedIdAuthorized && state.IsTargetIdPresent;

            var fullNameDirty = _lastFullName != null && FullNameLineEdit.Text != state.TargetIdFullName;
            var jobTitleDirty = _lastJobTitle != null && JobTitleLineEdit.Text != state.TargetIdJobTitle;

            FullNameLabel.Modulate = interfaceEnabled ? Color.White : Color.Gray;
            FullNameLineEdit.Editable = interfaceEnabled;
            if (!fullNameDirty)
            {
                FullNameLineEdit.Text = state.TargetIdFullName ?? string.Empty;
            }

            FullNameSaveButton.Disabled = !interfaceEnabled || !fullNameDirty;

            JobTitleLabel.Modulate = interfaceEnabled ? Color.White : Color.Gray;
            JobTitleLineEdit.Editable = interfaceEnabled;
            if (!jobTitleDirty)
            {
                JobTitleLineEdit.Text = state.TargetIdJobTitle ?? string.Empty;
            }

            JobTitleSaveButton.Disabled = !interfaceEnabled || !jobTitleDirty;

            foreach (var (accessName, button) in _accessButtons)
            {
                button.Disabled = !interfaceEnabled;
                if (interfaceEnabled)
                {
                    button.Pressed = state.TargetIdAccessList?.Contains(accessName) ?? false;
                }
            }

            _lastFullName = state.TargetIdFullName;
            _lastJobTitle = state.TargetIdJobTitle;
        }

        private void SubmitData()
        {
            _owner.SubmitData(
                FullNameLineEdit.Text,
                JobTitleLineEdit.Text,
                // Iterate over the buttons dictionary, filter by `Pressed`, only get key from the key/value pair
                _accessButtons.Where(x => x.Value.Pressed).Select(x => x.Key).ToList());
        }
    }
}
