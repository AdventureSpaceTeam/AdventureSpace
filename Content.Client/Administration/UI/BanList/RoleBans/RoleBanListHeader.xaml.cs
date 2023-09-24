﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI.BanList.RoleBans;

[GenerateTypedNameReferences]
public sealed partial class RoleBanListHeader : ContainerButton
{
    public RoleBanListHeader()
    {
        RobustXamlLoader.Load(this);
    }
}
