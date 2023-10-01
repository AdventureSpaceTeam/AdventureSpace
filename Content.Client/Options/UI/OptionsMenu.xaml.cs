using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Content.Client.Options.UI.Tabs;


namespace Content.Client.Options.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class OptionsMenu : DefaultWindow
    {
        public OptionsMenu()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            Tabs.SetTabTitle(0, Loc.GetString("ui-options-tab-graphics"));
            Tabs.SetTabTitle(1, Loc.GetString("ui-options-tab-controls"));
            Tabs.SetTabTitle(2, Loc.GetString("ui-options-tab-audio"));
            Tabs.SetTabTitle(3, Loc.GetString("ui-options-tab-network"));

            UpdateTabs();
        }

        public void UpdateTabs()
        {
            GraphicsTab.UpdateProperties();
        }
    }
}
