﻿using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI.Tabs.ObjectsTab;

[GenerateTypedNameReferences]
public sealed partial class ObjectsTabEntry : PanelContainer
{
    public NetEntity AssocEntity;

    public ObjectsTabEntry(string name, NetEntity nent, StyleBox styleBox)
    {
        RobustXamlLoader.Load(this);
        AssocEntity = nent;
        EIDLabel.Text = nent.ToString();
        NameLabel.Text = name;
        BackgroundColorPanel.PanelOverride = styleBox;
    }
}
