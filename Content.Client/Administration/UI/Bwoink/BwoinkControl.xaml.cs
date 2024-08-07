using System.Linq;
using System.Text;
using Content.Client.Administration.Managers;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;

namespace Content.Client.Administration.UI.Bwoink
{
    /// <summary>
    /// This window connects to a BwoinkSystem channel. BwoinkSystem manages the rest.
    /// </summary>
    [GenerateTypedNameReferences]
    public sealed partial class BwoinkControl : Control
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IClientConsoleHost _console = default!;
        [Dependency] private readonly IUserInterfaceManager _ui = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        public AdminAHelpUIHandler AHelpHelper = default!;

        private PlayerInfo? _currentPlayer;
        private readonly Dictionary<Button, ConfirmationData> _confirmations = new();

        public BwoinkControl()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            var uiController = _ui.GetUIController<AHelpUIController>();
            if (uiController.UIHelper is not AdminAHelpUIHandler helper)
                return;

            AHelpHelper = helper;

            _adminManager.AdminStatusUpdated += UpdateButtons;
            UpdateButtons();

            ChannelSelector.OnSelectionChanged += sel =>
            {
                _currentPlayer = sel;
                SwitchToChannel(sel?.SessionId);
                ChannelSelector.PlayerListContainer.DirtyList();
            };

            ChannelSelector.OverrideText += (info, text) =>
            {
                var sb = new StringBuilder();

                // Alteros-Sponsors-start
                if (info.IsSponsor)
                    sb.Append("★");
                // Alteros-Sponsors-end

                if (info.Connected)
                    sb.Append('●');
                else
                    sb.Append(info.ActiveThisRound ? '○' : '·');

                sb.Append(' ');
                if (AHelpHelper.TryGetChannel(info.SessionId, out var panel) && panel.Unread > 0)
                {
                    if (panel.Unread < 11)
                        sb.Append(new Rune('➀' + (panel.Unread-1)));
                    else
                        sb.Append(new Rune(0x2639)); // ☹
                    sb.Append(' ');
                }

                if (info.Antag && info.ActiveThisRound)
                    sb.Append(new Rune(0x1F5E1)); // 🗡

                if (info.OverallPlaytime <= TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.NewPlayerThreshold)))
                    sb.Append(new Rune(0x23F2)); // ⏲

                sb.AppendFormat("\"{0}\"", text);

                return sb.ToString();
            };

            ChannelSelector.Comparison = (a, b) =>
            {
                var ach = AHelpHelper.EnsurePanel(a.SessionId);
                var bch = AHelpHelper.EnsurePanel(b.SessionId);

                // Pinned players first
                if (a.IsPinned != b.IsPinned)
                    return a.IsPinned ? -1 : 1;

                // First, sort by unread. Any chat with unread messages appears first. We just sort based on unread
                // status, not number of unread messages, so that more recent unread messages take priority.
                var aUnread = ach.Unread > 0;
                var bUnread = bch.Unread > 0;
                if (aUnread != bUnread)
                    return aUnread ? -1 : 1;

                // Next, sort by connection status. Any disconnected players are grouped towards the end.
                if (a.Connected != b.Connected)
                    return a.Connected ? -1 : 1;

                // Sort connected players by New Player status, then by Antag status
                if (a.Connected && b.Connected)
                {
                    var aNewPlayer = a.OverallPlaytime <= TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.NewPlayerThreshold));
                    var bNewPlayer = b.OverallPlaytime <= TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.NewPlayerThreshold));

                    if (aNewPlayer != bNewPlayer)
                        return aNewPlayer ? -1 : 1;

                    if (a.Antag != b.Antag)
                        return a.Antag ? -1 : 1;
                }

                // Sort disconnected players by participation in the round
                if (!a.Connected && !b.Connected)
                {
                    if (a.ActiveThisRound != b.ActiveThisRound)
                        return a.ActiveThisRound ? -1 : 1;
                }

                // Finally, sort by the most recent message.
                return bch.LastMessage.CompareTo(ach.LastMessage);
            };


            Bans.OnPressed += _ =>
            {
                if (_currentPlayer is not null)
                    _console.ExecuteCommand($"banlist \"{_currentPlayer.SessionId}\"");
            };

            Notes.OnPressed += _ =>
            {
                if (_currentPlayer is not null)
                    _console.ExecuteCommand($"adminnotes \"{_currentPlayer.SessionId}\"");
            };

            Ban.OnPressed += _ =>
            {
                if (_currentPlayer is not null)
                    _console.ExecuteCommand($"banpanel \"{_currentPlayer.SessionId}\"");
            };

            Kick.OnPressed += _ =>
            {
                if (!AdminUIHelpers.TryConfirm(Kick, _confirmations))
                {
                    return;
                }

                // TODO: Reason field
                if (_currentPlayer is not null)
                    _console.ExecuteCommand($"kick \"{_currentPlayer.Username}\"");
            };

            Follow.OnPressed += _ =>
            {
                if (_currentPlayer is not null)
                    _console.ExecuteCommand($"follow \"{_currentPlayer.NetEntity}\"");
            };

            Respawn.OnPressed += _ =>
            {
                if (!AdminUIHelpers.TryConfirm(Respawn, _confirmations))
                {
                    return;
                }

                if (_currentPlayer is not null)
                    _console.ExecuteCommand($"respawn \"{_currentPlayer.Username}\"");
            };

            PopOut.OnPressed += _ =>
            {
                uiController.PopOut();
            };
        }

        public void OnBwoink(NetUserId channel)
        {
            ChannelSelector.PopulateList();
        }


        public void SelectChannel(NetUserId channel)
        {
            if (!ChannelSelector.PlayerInfo.TryFirstOrDefault(
                i => i.SessionId == channel, out var info))
                return;

            // clear filter if we're trying to select a channel for a player that isn't currently filtered
            // i.e. through the message verb.
            var data = new PlayerListData(info);
            if (!ChannelSelector.PlayerListContainer.Data.Contains(data))
            {
                ChannelSelector.StopFiltering();
            }

            ChannelSelector.PopulateList();
            ChannelSelector.PlayerListContainer.Select(data);
        }

        public void UpdateButtons()
        {
            var disabled = _currentPlayer == null;

            Bans.Visible = _adminManager.HasFlag(AdminFlags.Ban);
            Bans.Disabled = !Bans.Visible || disabled;

            Notes.Visible = _adminManager.HasFlag(AdminFlags.ViewNotes);
            Notes.Disabled = !Notes.Visible || disabled;

            Ban.Visible = _adminManager.HasFlag(AdminFlags.Ban);
            Ban.Disabled = !Ban.Visible || disabled;

            Kick.Visible = _adminManager.CanCommand("kick");
            Kick.Disabled = !Kick.Visible || disabled;

            Respawn.Visible = _adminManager.CanCommand("respawn");
            Respawn.Disabled = !Respawn.Visible || disabled;

            Follow.Visible = _adminManager.CanCommand("follow");
            Follow.Disabled = !Follow.Visible || disabled;
        }

        private string FormatTabTitle(ItemList.Item li, PlayerInfo? pl = default)
        {
            pl ??= (PlayerInfo) li.Metadata!;
            var sb = new StringBuilder();
            sb.Append(pl.Connected ? '●' : '○');
            sb.Append(' ');
            if (AHelpHelper.TryGetChannel(pl.SessionId, out var panel) && panel.Unread > 0)
            {
                if (panel.Unread < 11)
                    sb.Append(new Rune('➀' + (panel.Unread-1)));
                else
                    sb.Append(new Rune(0x2639)); // ☹
                sb.Append(' ');
            }

            if (pl.Antag)
                sb.Append(new Rune(0x1F5E1)); // 🗡

            if (pl.OverallPlaytime <= TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.NewPlayerThreshold)))
                sb.Append(new Rune(0x23F2)); // ⏲

            sb.AppendFormat("\"{0}\"", pl.CharacterName);

            if (pl.IdentityName != pl.CharacterName && pl.IdentityName != string.Empty)
                sb.Append(' ').AppendFormat("[{0}]", pl.IdentityName);

            sb.Append(' ').Append(pl.Username);

            return sb.ToString();
        }

        private void SwitchToChannel(NetUserId? ch)
        {
            UpdateButtons();

            AHelpHelper.HideAllPanels();
            if (ch != null)
            {
                var panel = AHelpHelper.EnsurePanel(ch.Value);
                panel.Visible = true;
            }
        }

        public void PopulateList()
        {
            // Maintain existing pin statuses
            var pinnedPlayers = ChannelSelector.PlayerInfo.Where(p => p.IsPinned).ToDictionary(p => p.SessionId);

            ChannelSelector.PopulateList();

            // Restore pin statuses
            foreach (var player in ChannelSelector.PlayerInfo)
            {
                if (pinnedPlayers.TryGetValue(player.SessionId, out var pinnedPlayer))
                {
                    player.IsPinned = pinnedPlayer.IsPinned;
                }
            }

            UpdateButtons();
        }
    }
}
