﻿using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.Tabs.AdminTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public partial class KickWindow : SS14Window
    {
        private ICommonSession? _selectedSession;

        protected override void EnteredTree()
        {
            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
            PlayerList.OnSelectionChanged += OnListOnOnSelectionChanged;
        }

        private void OnListOnOnSelectionChanged(ICommonSession? obj)
        {
            _selectedSession = obj;
            SubmitButton.Disabled = _selectedSession == null;
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (_selectedSession == null)
                return;
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(
                $"kick \"{_selectedSession.Name}\" \"{CommandParsing.Escape(ReasonLine.Text)}\"");
        }
    }
}
