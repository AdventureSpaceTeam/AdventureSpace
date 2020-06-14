using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Client.UserInterface.Stylesheets;
using Newtonsoft.Json;
using Robust.Client.Credits;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

#nullable enable

namespace Content.Client.UserInterface
{
    public sealed class CreditsWindow : SS14Window
    {
        [Dependency] private readonly IResourceCache _resourceManager = default!;

        private static readonly Dictionary<string, int> PatronTierPriority = new Dictionary<string, int>
        {
            ["Nuclear Operative"] = 1,
            ["Syndicate Agent"] = 2,
            ["Revolutionary"] = 3
        };

        public CreditsWindow()
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Credits");

            var rootContainer = new TabContainer();

            var patronsList = new ScrollContainer();
            var ss14ContributorsList = new ScrollContainer();
            var licensesList = new ScrollContainer();

            rootContainer.AddChild(ss14ContributorsList);
            rootContainer.AddChild(patronsList);
            rootContainer.AddChild(licensesList);

            TabContainer.SetTabTitle(patronsList, Loc.GetString("Patrons"));
            TabContainer.SetTabTitle(ss14ContributorsList, Loc.GetString("Credits"));
            TabContainer.SetTabTitle(licensesList, Loc.GetString("Open Source Licenses"));

            PopulatePatronsList(patronsList);
            PopulateCredits(ss14ContributorsList);
            PopulateLicenses(licensesList);

            Contents.AddChild(rootContainer);

            CustomMinimumSize = (650, 450);
        }

        private void PopulateLicenses(ScrollContainer licensesList)
        {
            var margin = new MarginContainer {MarginLeftOverride = 2, MarginTopOverride = 2};
            var vBox = new VBoxContainer();
            margin.AddChild(vBox);

            foreach (var entry in CreditsManager.GetLicenses().OrderBy(p => p.Name))
            {
                vBox.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = entry.Name});

                // We split these line by line because otherwise
                // the LGPL causes Clyde to go out of bounds in the rendering code.
                foreach (var line in entry.License.Split("\n"))
                {
                    vBox.AddChild(new Label {Text = line, FontColorOverride = new Color(200, 200, 200)});
                }
            }

            licensesList.AddChild(margin);
        }

        private void PopulatePatronsList(Control patronsList)
        {
            var margin = new MarginContainer {MarginLeftOverride = 2, MarginTopOverride = 2};
            var vBox = new VBoxContainer();
            margin.AddChild(vBox);
            var patrons = ReadJson<PatronEntry[]>("/Credits/Patrons.json");

            Button patronButton;
            vBox.AddChild(patronButton = new Button
            {
                Text = "Become a Patron",
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            });

            var first = true;
            foreach (var tier in patrons.GroupBy(p => p.Tier).OrderBy(p => PatronTierPriority[p.Key]))
            {
                if (!first)
                {
                    vBox.AddChild(new Control {CustomMinimumSize = (0, 10)});
                }

                first = false;
                vBox.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = $"{tier.Key}"});

                var msg = string.Join(", ", tier.OrderBy(p => p.Name).Select(p => p.Name));

                var label = new RichTextLabel();
                label.SetMessage(msg);

                vBox.AddChild(label);
            }


            patronButton.OnPressed +=
                _ => IoCManager.Resolve<IUriOpener>().OpenUri(UILinks.Patreon);

            patronsList.AddChild(margin);
        }

        private void PopulateCredits(Control contributorsList)
        {
            Button contributeButton;

            var margin = new MarginContainer
            {
                MarginLeftOverride = 2,
                MarginTopOverride = 2
            };
            var vBox = new VBoxContainer();
            margin.AddChild(vBox);

            vBox.AddChild(new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SeparationOverride = 20,
                Children =
                {
                    new Label {Text = "Want to get on this list?"},
                    (contributeButton = new Button {Text = "Contribute!"})
                }
            });

            var first = true;
            void AddSection(string title, string path, bool markup = false)
            {
                if (!first)
                {
                    vBox.AddChild(new Control {CustomMinimumSize = (0, 10)});
                }

                first = false;
                vBox.AddChild(new Label {StyleClasses = {StyleBase.StyleClassLabelHeading}, Text = title});

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

                vBox.AddChild(label);
            }

            AddSection("Space Station 14 Contributors", "GitHub.txt");
            AddSection("Space Station 13 Codebases", "SpaceStation13.txt");
            AddSection("Original Space Station 13 Remake Team", "OriginalRemake.txt");
            AddSection("Special Thanks", "SpecialThanks.txt", true);

            contributorsList.AddChild(margin);

            contributeButton.OnPressed += _ =>
                IoCManager.Resolve<IUriOpener>().OpenUri(UILinks.GitHub);
        }

        private static IEnumerable<string> Lines(TextReader reader)
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    yield break;
                }

                yield return line;
            }
        }

        private T ReadJson<T>(string path)
        {
            var serializer = new JsonSerializer();

            using var stream = _resourceManager.ContentFileRead(path);
            using var streamReader = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(streamReader);

            return serializer.Deserialize<T>(jsonTextReader)!;
        }

        [JsonObject(ItemRequired = Required.Always)]
        private sealed class PatronEntry
        {
            public string Name { get; set; } = default!;
            public string Tier { get; set; } = default!;
        }

        [JsonObject(ItemRequired = Required.Always)]
        private sealed class OpenSourceLicense
        {
            public string Name { get; set; } = default!;
            public string License { get; set; } = default!;
        }
    }
}
