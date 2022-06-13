﻿using Content.Client.UserInterface;
using Content.Shared.DeviceNetwork;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.NetworkConfigurator;

[GenerateTypedNameReferences]
public sealed partial class NetworkConfiguratorListMenu : FancyWindow
{
    private readonly NetworkConfiguratorBoundUserInterface _ui;
    public NetworkConfiguratorListMenu(NetworkConfiguratorBoundUserInterface ui)
    {
        RobustXamlLoader.Load(this);

        _ui = ui;
    }

    public void UpdateState(NetworkConfiguratorUserInterfaceState state)
    {
        DeviceCountLabel.Text = Loc.GetString("network-configurator-ui-count-label", ("count", state.DeviceList.Count));
        DeviceList.RemoveAllChildren();

        foreach (var savedDevice in state.DeviceList)
        {
            DeviceList.AddChild(BuildDeviceListRow(savedDevice));
        }
    }

    private BoxContainer BuildDeviceListRow((string address, string name) savedDevice)
    {
        var row = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(8)
        };

        var name = new Label()
        {
            Text = savedDevice.name[..Math.Min(11, savedDevice.name.Length)],
            SetWidth = 84
        };

        var address = new Label()
        {
            Text = savedDevice.address,
            HorizontalExpand = true,
            Align = Label.AlignMode.Center
        };

        var removeButton = new TextureButton()
        {
            StyleClasses = { "CrossButtonRed" },
            VerticalAlignment = VAlignment.Center,
            Scale = new Vector2(0.5f, 0.5f)
        };

        removeButton.OnPressed += _ => _ui.OnRemoveButtonPressed(savedDevice.address);

        row.AddChild(name);
        row.AddChild(address);
        row.AddChild(removeButton);

        return row;
    }
}
