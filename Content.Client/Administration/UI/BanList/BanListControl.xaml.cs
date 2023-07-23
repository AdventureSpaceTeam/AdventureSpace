﻿using System.Linq;
using System.Numerics;
using Content.Client.Administration.UI.CustomControls;
using Content.Shared.Administration.BanList;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI.BanList;

[GenerateTypedNameReferences]
public sealed partial class BanListControl : Control
{
    private BanListIdsPopup? _popup;

    public BanListControl()
    {
        RobustXamlLoader.Load(this);
    }

    public void SetBans(List<SharedServerBan> bans)
    {
        foreach (var control in Bans.Children.ToArray()[1..])
        {
            control.Orphan();
        }

        foreach (var ban in bans)
        {
            Bans.AddChild(new HSeparator());

            var line = new BanListLine(ban);
            line.OnIdsClicked += LineIdsClicked;

            Bans.AddChild(line);
        }
    }

    private void ClosePopup()
    {
        _popup?.Close();
        _popup = null;
    }

    private bool LineIdsClicked(BanListLine line)
    {
        ClosePopup();

        var ban = line.Ban;
        var id = ban.Id == null ? string.Empty : Loc.GetString("ban-list-id", ("id", ban.Id.Value));
        var ip = ban.Address == null
            ? string.Empty
            : Loc.GetString("ban-list-ip", ("ip", ban.Address.Value.address));
        var hwid = ban.HWId == null ? string.Empty : Loc.GetString("ban-list-hwid", ("hwid", ban.HWId));
        var guid = ban.UserId == null
            ? string.Empty
            : Loc.GetString("ban-list-guid", ("guid", ban.UserId.Value.ToString()));

        _popup = new BanListIdsPopup(id, ip, hwid, guid);

        var box = UIBox2.FromDimensions(UserInterfaceManager.MousePositionScaled.Position, new Vector2(1, 1));
        _popup.Open(box);

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_popup != null)
        {
            UserInterfaceManager.PopupRoot.RemoveChild(_popup);
        }
    }
}
