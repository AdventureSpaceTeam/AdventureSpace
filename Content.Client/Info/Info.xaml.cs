﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Info;

[GenerateTypedNameReferences]
public partial class Info : ScrollContainer
{
    public Info()
    {
        RobustXamlLoader.Load(this);
    }
}
