﻿using Content.Shared.Research.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Research.UI;

[GenerateTypedNameReferences]
public sealed partial class TechnologyCardControl : Control
{
    public Action? OnPressed;

    public TechnologyCardControl(TechnologyPrototype technology, IPrototypeManager prototypeManager, SpriteSystem spriteSys, FormattedMessage description, int points, bool hasAccess)
    {
        RobustXamlLoader.Load(this);

        var discipline = prototypeManager.Index<TechDisciplinePrototype>(technology.Discipline);
        Background.ModulateSelfOverride = discipline.Color;

        DisciplineTexture.Texture = spriteSys.Frame0(discipline.Icon);
        TechnologyNameLabel.Text = Loc.GetString(technology.Name);
        var message = new FormattedMessage();
        message.AddMarkupOrThrow(Loc.GetString("research-console-tier-discipline-info",
            ("tier", technology.Tier), ("color", discipline.Color), ("discipline", Loc.GetString(discipline.Name))));
        TierLabel.SetMessage(message);
        UnlocksLabel.SetMessage(description);

        TechnologyTexture.Texture = spriteSys.Frame0(technology.Icon);

        if (!hasAccess)
            ResearchButton.ToolTip = Loc.GetString("research-console-no-access-popup");

        ResearchButton.Disabled = points < technology.Cost || !hasAccess;
        ResearchButton.OnPressed += _ => OnPressed?.Invoke();
    }
}
