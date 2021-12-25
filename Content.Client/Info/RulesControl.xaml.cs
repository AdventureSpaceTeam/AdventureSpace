﻿using Robust.Client.AutoGenerated;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Info;

[GenerateTypedNameReferences]
public partial class RulesControl : BoxContainer
{
    [Dependency] private readonly IResourceCache _resourceManager = default!;

    public RulesControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        AddChild(new InfoSection(Loc.GetString("ui-rules-header"),
            _resourceManager.ContentFileReadAllText($"/Server Info/Rules.txt"), true));
    }
}
