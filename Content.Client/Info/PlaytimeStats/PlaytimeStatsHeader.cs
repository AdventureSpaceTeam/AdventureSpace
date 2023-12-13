using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Input;

namespace Content.Client.Info.PlaytimeStats;

[GenerateTypedNameReferences]
public sealed partial class PlaytimeStatsHeader : Control
{
    public event Action<Header, SortDirection>? OnHeaderClicked;
    private SortDirection _roleDirection = SortDirection.Ascending;
    private SortDirection _playtimeDirection = SortDirection.Descending;

    public PlaytimeStatsHeader()
    {
        RobustXamlLoader.Load(this);

        RoleLabel.OnKeyBindDown += RoleClicked;
        PlaytimeLabel.OnKeyBindDown += PlaytimeClicked;

        UpdateLabels();
    }

    public enum Header : byte
    {
        Role,
        Playtime
    }
    public enum SortDirection : byte
    {
        Ascending,
        Descending
    }

    private void HeaderClicked(GUIBoundKeyEventArgs args, Header header)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
        {
            return;
        }

        switch (header)
        {
            case Header.Role:
                _roleDirection = _roleDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                break;
            case Header.Playtime:
                _playtimeDirection = _playtimeDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                break;
        }

        UpdateLabels();
        OnHeaderClicked?.Invoke(header, header == Header.Role ? _roleDirection : _playtimeDirection);
        args.Handle();
    }
    private void UpdateLabels()
    {
        RoleLabel.Text = Loc.GetString("ui-playtime-header-role-type") +
                         (_roleDirection == SortDirection.Ascending ? " ↓" : " ↑");
        PlaytimeLabel.Text = Loc.GetString("ui-playtime-header-role-time") +
                             (_playtimeDirection == SortDirection.Ascending ? " ↓" : " ↑");
    }

    private void RoleClicked(GUIBoundKeyEventArgs args)
    {
        HeaderClicked(args, Header.Role);
    }

    private void PlaytimeClicked(GUIBoundKeyEventArgs args)
    {
        HeaderClicked(args, Header.Playtime);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            RoleLabel.OnKeyBindDown -= RoleClicked;
            PlaytimeLabel.OnKeyBindDown -= PlaytimeClicked;
        }
    }
}
