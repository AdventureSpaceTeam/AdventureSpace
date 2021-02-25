﻿using System;
using System.Linq;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Content.Client.Changelog.ChangelogManager;

namespace Content.Client.Changelog
{
    [GenerateTypedNameReferences]
    public sealed partial class ChangelogWindow : BaseWindow
    {
        [Dependency] private readonly ChangelogManager _changelog = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public ChangelogWindow()
        {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            Stylesheet = IoCManager.Resolve<IStylesheetManager>().SheetSpace;
            CloseButton.OnPressed += _ => Close();
        }

        protected override void Opened()
        {
            base.Opened();

            _changelog.SaveNewReadId();
            PopulateChangelog();
        }

        private async void PopulateChangelog()
        {
            // Changelog is not kept in memory so load it again.
            var changelog = await _changelog.LoadChangelog();

            var byDay = changelog
                .GroupBy(e => e.Time.ToLocalTime().Date)
                .OrderByDescending(c => c.Key);

            var hasRead = _changelog.MaxId <= _changelog.LastReadId;
            foreach (var dayEntries in byDay)
            {
                var day = dayEntries.Key;

                var groupedEntries = dayEntries
                    .GroupBy(c => (c.Author, Read: c.Id <= _changelog.LastReadId))
                    .OrderBy(c => c.Key.Read)
                    .ThenBy(c => c.Key.Author);

                string dayNice;
                var today = DateTime.Today;
                if (day == today)
                    dayNice = Loc.GetString("changelog-today");
                else if (day == today.AddDays(-1))
                    dayNice = Loc.GetString("changelog-yesterday");
                else
                    dayNice = day.ToShortDateString();

                ChangelogBody.AddChild(new Label
                {
                    Text = dayNice,
                    StyleClasses = {"LabelHeading"},
                    Margin = new Thickness(4, 6, 0, 0)
                });

                var first = true;

                foreach (var groupedEntry in groupedEntries)
                {
                    var (author, read) = groupedEntry.Key;

                    if (!first)
                    {
                        ChangelogBody.AddChild(new Control {Margin = new Thickness(4)});
                    }

                    if (read && !hasRead)
                    {
                        hasRead = true;

                        var upArrow =
                            _resourceCache.GetTexture("/Textures/Interface/Changelog/up_arrow.svg.192dpi.png");

                        var readDivider = new VBoxContainer();

                        var hBox = new HBoxContainer
                        {
                            HorizontalAlignment = HAlignment.Center,
                            Children =
                            {
                                new TextureRect
                                {
                                    Texture = upArrow,
                                    ModulateSelfOverride = Color.FromHex("#888"),
                                    TextureScale = (0.5f, 0.5f),
                                    Margin = new Thickness(4, 3),
                                    VerticalAlignment = VAlignment.Bottom
                                },
                                new Label
                                {
                                    Align = Label.AlignMode.Center,
                                    Text = Loc.GetString("changelog-new-changes"),
                                    FontColorOverride = Color.FromHex("#888"),
                                },
                                new TextureRect
                                {
                                    Texture = upArrow,
                                    ModulateSelfOverride = Color.FromHex("#888"),
                                    TextureScale = (0.5f, 0.5f),
                                    Margin = new Thickness(4, 3),
                                    VerticalAlignment = VAlignment.Bottom
                                }
                            }
                        };

                        readDivider.AddChild(hBox);
                        readDivider.AddChild(new PanelContainer {StyleClasses = {"LowDivider"}});
                        ChangelogBody.AddChild(readDivider);

                        if (first)
                            readDivider.SetPositionInParent(ChangelogBody.ChildCount - 2);
                    }

                    first = false;

                    var authorLabel = new RichTextLabel
                    {
                        Margin = new Thickness(6, 0, 0, 0),
                    };
                    authorLabel.SetMessage(
                        FormattedMessage.FromMarkup(Loc.GetString("changelog-author-changed", ("author", author))));
                    ChangelogBody.AddChild(authorLabel);

                    foreach (var change in groupedEntry.SelectMany(c => c.Changes))
                    {
                        var text = new RichTextLabel();
                        text.SetMessage(FormattedMessage.FromMarkup(change.Message));
                        ChangelogBody.AddChild(new HBoxContainer
                        {
                            Margin = new Thickness(14, 1, 10, 2),
                            Children =
                            {
                                GetIcon(change.Type),
                                text
                            }
                        });
                    }
                }
            }

            var version = typeof(ChangelogWindow).Assembly.GetName().Version ?? new Version(1, 0);
            VersionLabel.Text = Loc.GetString("changelog-version-tag", ("version", version.ToString()));
        }

        private TextureRect GetIcon(ChangelogLineType type)
        {
            var (file, color) = type switch
            {
                ChangelogLineType.Add => ("plus.svg.192dpi.png", "#6ED18D"),
                ChangelogLineType.Remove => ("minus.svg.192dpi.png", "#D16E6E"),
                ChangelogLineType.Fix => ("bug.svg.192dpi.png", "#D1BA6E"),
                ChangelogLineType.Tweak => ("wrench.svg.192dpi.png", "#6E96D1"),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            return new TextureRect
            {
                Texture = _resourceCache.GetTexture(new ResourcePath($"/Textures/Interface/Changelog/{file}")),
                VerticalAlignment = VAlignment.Top,
                TextureScale = (0.5f, 0.5f),
                Margin = new Thickness(2, 4, 6, 2),
                ModulateSelfOverride = Color.FromHex(color)
            };
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.Move;
        }
    }

    [UsedImplicitly]
    public sealed class ChangelogCommand : IConsoleCommand
    {
        public string Command => "changelog";
        public string Description => "Opens the changelog";
        public string Help => "Usage: changelog";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            new ChangelogWindow().OpenCentered();
        }
    }
}
