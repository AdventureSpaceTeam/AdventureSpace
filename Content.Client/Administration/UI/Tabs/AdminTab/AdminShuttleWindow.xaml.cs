using System;
using Content.Shared.Localizations;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Administration.UI.Tabs.AdminTab
{
    [GenerateTypedNameReferences]
    public sealed partial class AdminShuttleWindow : DefaultWindow
    {
        public AdminShuttleWindow()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            _callShuttleTime.OnTextChanged += CallShuttleTimeOnOnTextChanged;
        }

        private void CallShuttleTimeOnOnTextChanged(LineEdit.LineEditEventArgs obj)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            _callShuttleButton.Disabled = !TimeSpan.TryParseExact(obj.Text, ContentLocalizationManager.TimeSpanMinutesFormats, loc.DefaultCulture, out _);
            _callShuttleButton.Command = $"callshuttle {obj.Text}";
        }
    }
}
