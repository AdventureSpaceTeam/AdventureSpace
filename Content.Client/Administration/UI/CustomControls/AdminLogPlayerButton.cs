﻿using System;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public class AdminLogPlayerButton : Button
{
    public AdminLogPlayerButton(Guid id)
    {
        Id = id;
        ClipText = true;
        ToggleMode = true;
        Pressed = true;
    }

    public Guid Id { get; }
}
