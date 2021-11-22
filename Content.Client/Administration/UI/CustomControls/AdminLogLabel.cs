﻿using Content.Shared.Administration.Logs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public class AdminLogLabel : RichTextLabel
{
    public AdminLogLabel(ref SharedAdminLog log, HSeparator separator)
    {
        Log = log;
        Separator = separator;

        SetMessage(log.Message);
        OnVisibilityChanged += VisibilityChanged;
    }

    public SharedAdminLog Log { get; }

    public HSeparator Separator { get; }

    private void VisibilityChanged(Control control)
    {
        Separator.Visible = Visible;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        OnVisibilityChanged -= VisibilityChanged;
    }
}
