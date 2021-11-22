﻿using Content.Shared.Administration.Logs;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public class AdminLogImpactButton : Button
{
    public AdminLogImpactButton(LogImpact impact)
    {
        Impact = impact;
        ToggleMode = true;
        Pressed = true;
    }

    public LogImpact Impact { get; }
}
