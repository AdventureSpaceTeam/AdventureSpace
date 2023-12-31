﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Systems.Hotbar.Widgets;

[GenerateTypedNameReferences]
public sealed partial class HotbarGui : UIWidget
{
    public HotbarGui()
    {
        RobustXamlLoader.Load(this);
        StatusPanel.Update(null);
        var hotbarController = UserInterfaceManager.GetUIController<HotbarUIController>();

        hotbarController.Setup(HandContainer, StatusPanel, StoragePanel);
        LayoutContainer.SetGrowVertical(this, LayoutContainer.GrowDirection.Begin);
    }

    public void UpdatePanelEntity(EntityUid? entity)
    {
        StatusPanel.Update(entity);
        if (entity == null)
        {
            StatusPanel.Visible = false;
            return;
        }

        StatusPanel.Visible = true;
    }
}
