using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Robust.Client.AutoGenerated;
using Robust.Client.Credits;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Credits
{
    [GenerateTypedNameReferences]
    public sealed partial class CreditsWindow : DefaultWindow
    {
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private static readonly Dictionary<string, int> PatronTierPriority = new()
        {
            ["Nuclear Operative"] = 1,
            ["Syndicate Agent"] = 2,
            ["Revolutionary"] = 3
        };

        public CreditsWindow()
        {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            TabContainer.SetTabTitle(Ss14ContributorsTab, Loc.GetString("credits-window-ss14contributorslist-tab"));
            TabContainer.SetTabTitle(PatronsTab, Loc.GetString("credits-window-patrons-tab"));
            TabContainer.SetTabTitle(LicensesTab, Loc.GetString("credits-window-licenses-tab"));

            PopulateContributors(Ss14ContributorsContainer);
            PopulatePatrons(PatronsContainer);
            PopulateLicenses(LicensesContainer);
        }

        private void PopulateLicenses(BoxContainer licensesContainer)
        {
            foreach (var entry in CreditsManager.GetLicenses(_resourceManager).OrderBy(p => p.Name))
            {
                licensesContainer.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = entry.Name});

                // We split these line by line because otherwise
                // the LGPL causes Clyde to go out of bounds in the rendering code.
                foreach (var line in entry.License.Split("\n"))
                {
                    licensesContainer.AddChild(new Label {Text = line, FontColorOverride = new Color(200, 200, 200)});
                }
            }
        }

        private void PopulatePatrons(BoxContainer patronsContainer)
        {
            var patrons = LoadPatrons();

            // Do not show "become a patron" button on Steam builds
            // since Patreon violates Valve's rules about alternative storefronts.
            var linkPatreon = _cfg.GetCVar(CCVars.InfoLinksPatreon);
            if (!_cfg.GetCVar(CCVars.BrandingSteam) && linkPatreon != "")
            {
                Button patronButton;
                patronsContainer.AddChild(patronButton = new Button
                {
                    Text = Loc.GetString("credits-window-become-patron-button"),
                    HorizontalAlignment = HAlignment.Center
                });

                patronButton.OnPressed +=
                    _ => IoCManager.Resolve<IUriOpener>().OpenUri(linkPatreon);
            }

            var first = true;
            foreach (var tier in patrons.GroupBy(p => p.Tier).OrderBy(p => PatronTierPriority[p.Key]))
            {
                if (!first)
                {
                    patronsContainer.AddChild(new Control {MinSize = new Vector2(0, 10)});
                }

                first = false;
                patronsContainer.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = $"{tier.Key}"});

                var msg = string.Join(", ", tier.OrderBy(p => p.Name).Select(p => p.Name));

                var label = new RichTextLabel();
                label.SetMessage(msg);

                patronsContainer.AddChild(label);
            }
        }

        private IEnumerable<PatronEntry> LoadPatrons()
        {
            var yamlStream = _resourceManager.ContentFileReadYaml(new ("/Credits/Patrons.yml"));
            var sequence = (YamlSequenceNode) yamlStream.Documents[0].RootNode;

            return sequence
                .Cast<YamlMappingNode>()
                .Select(m => new PatronEntry(m["Name"].AsString(), m["Tier"].AsString()));
        }

        private void PopulateContributors(BoxContainer ss14ContributorsContainer)
        {
            Button contributeButton;

            ss14ContributorsContainer.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalAlignment = HAlignment.Center,
                SeparationOverride = 20,
                Children =
                {
                    new Label {Text = Loc.GetString("credits-window-contributor-encouragement-label") },
                    (contributeButton = new Button {Text = Loc.GetString("credits-window-contribute-button")})
                }
            });

            var first = true;

            void AddSection(string title, string path, bool markup = false)
            {
                if (!first)
                {
                    ss14ContributorsContainer.AddChild(new Control {MinSize = new Vector2(0, 10)});
                }

                first = false;
                ss14ContributorsContainer.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = title});

                var label = new RichTextLabel();
                var text = _resourceManager.ContentFileReadAllText($"/Credits/{path}");
                if (markup)
                {
                    label.SetMessage(FormattedMessage.FromMarkup(text.Trim()));
                }
                else
                {
                    label.SetMessage(text);
                }

                ss14ContributorsContainer.AddChild(label);
            }

            AddSection(Loc.GetString("credits-window-contributors-section-title"), "GitHub.txt");
            AddSection(Loc.GetString("credits-window-codebases-section-title"), "SpaceStation13.txt");
            AddSection(Loc.GetString("credits-window-original-remake-team-section-title"), "OriginalRemake.txt");
            AddSection(Loc.GetString("credits-window-special-thanks-section-title"), "SpecialThanks.txt", true);

            var linkGithub = _cfg.GetCVar(CCVars.InfoLinksGithub);

            contributeButton.OnPressed += _ =>
                IoCManager.Resolve<IUriOpener>().OpenUri(linkGithub);

            if (linkGithub == "")
                contributeButton.Visible = false;
        }

        private sealed class PatronEntry
        {
            public string Name { get; }
            public string Tier { get; }

            public PatronEntry(string name, string tier)
            {
                Name = name;
                Tier = tier;
            }
        }
    }
}
