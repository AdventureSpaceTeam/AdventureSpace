﻿using Content.Shared.Administration.Logs;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public class AdminLogTypeButton : Button
{
    public AdminLogTypeButton(LogType type)
    {
        Type = type;
        ClipText = true;
        ToggleMode = true;
    }

    public LogType Type { get; }
}
