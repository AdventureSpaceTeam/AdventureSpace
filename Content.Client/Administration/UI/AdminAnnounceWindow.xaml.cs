using Content.Client.HUD;
using Content.Shared.Administration;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Administration.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class AdminAnnounceWindow : DefaultWindow
    {
        [Dependency] private readonly ILocalizationManager _localization = default!;

        public AdminAnnounceWindow()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            AnnounceMethod.AddItem(_localization.GetString("announce-type-station"));
            AnnounceMethod.SetItemMetadata(0, AdminAnnounceType.Station);
            AnnounceMethod.AddItem(_localization.GetString("announce-type-server"));
            AnnounceMethod.SetItemMetadata(1, AdminAnnounceType.Server);
            AnnounceMethod.OnItemSelected += AnnounceMethodOnOnItemSelected;
            Announcement.OnTextChanged += AnnouncementOnOnTextChanged;
        }


        private void AnnouncementOnOnTextChanged(LineEdit.LineEditEventArgs args)
        {
            AnnounceButton.Disabled = args.Text.TrimStart() == "";
        }

        private void AnnounceMethodOnOnItemSelected(OptionButton.ItemSelectedEventArgs args)
        {
            AnnounceMethod.SelectId(args.Id);
            Announcer.Editable = ((AdminAnnounceType?)args.Button.SelectedMetadata ?? AdminAnnounceType.Station) == AdminAnnounceType.Station;
        }
    }
}
