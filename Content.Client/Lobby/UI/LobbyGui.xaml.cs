using Content.Client.Chat.UI;
using Content.Client.Info;
using Content.Client.Preferences;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI
{
    [GenerateTypedNameReferences]
    internal sealed partial class LobbyGui : Control
    {
        public LobbyCharacterPreviewPanel CharacterPreview { get; }

        public LobbyGui(IEntityManager entityManager,
            IClientPreferencesManager preferencesManager)
        {
            RobustXamlLoader.Load(this);

            ServerName.HorizontalExpand = true;
            ServerName.HorizontalAlignment = HAlignment.Center;

            CharacterPreview = new LobbyCharacterPreviewPanel(
                entityManager,
                preferencesManager,
                IoCManager.Resolve<IPrototypeManager>())
            {
                HorizontalAlignment = HAlignment.Left
            };

            LeftPanelContainer.AddChild(CharacterPreview);
            CharacterPreview.SetPositionFirst();
        }
    }

    public sealed class LobbyPlayerList : Control
    {
        private readonly ScrollContainer _scroll;
        private readonly BoxContainer _vBox;

        public LobbyPlayerList()
        {
            var panel = new PanelContainer()
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#202028") },
            };
            _vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            _scroll = new ScrollContainer();
            _scroll.AddChild(_vBox);
            panel.AddChild(_scroll);
            AddChild(panel);
        }

        // Adds a row
        public void AddItem(string name, string status)
        {
            var hbox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
            };

            // Player Name
            hbox.AddChild(new PanelContainer()
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#373744"),
                    ContentMarginBottomOverride = 2,
                    ContentMarginLeftOverride = 4,
                    ContentMarginRightOverride = 4,
                    ContentMarginTopOverride = 2
                },
                Children =
                {
                    new Label
                    {
                        Text = name
                    }
                },
                HorizontalExpand = true
            });
            // Status
            hbox.AddChild(new PanelContainer()
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#373744"),
                    ContentMarginBottomOverride = 2,
                    ContentMarginLeftOverride = 4,
                    ContentMarginRightOverride = 4,
                    ContentMarginTopOverride = 2
                },
                Children =
                {
                    new Label
                    {
                        Text = status
                    }
                },
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 0.2f,
            });

            _vBox.AddChild(hbox);
        }

        // Deletes all rows
        public void Clear()
        {
            _vBox.RemoveAllChildren();
        }
    }
}
