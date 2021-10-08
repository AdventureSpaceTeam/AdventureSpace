using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Lobby.UI
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    internal sealed partial class ObserveWarningWindow : SS14Window
    {
        public ObserveWarningWindow()
        {
            Title = Loc.GetString("observe-warning-window-title");
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            ObserveButton.OnPressed += _ => { this.Close(); };
            NevermindButton.OnPressed += _ => { this.Close(); };
        }
    }
}
