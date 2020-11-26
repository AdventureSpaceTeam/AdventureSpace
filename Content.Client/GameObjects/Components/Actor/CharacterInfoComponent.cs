#nullable enable
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects.Components.Actor;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Actor
{
    [RegisterComponent]
    public sealed class CharacterInfoComponent : SharedCharacterInfoComponent, ICharacterUI
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private CharacterInfoControl _control = default!;

        public Control Scene { get; private set; } = default!;
        public UIPriority Priority => UIPriority.Info;

        public override void OnAdd()
        {
            base.OnAdd();

            Scene = _control = new CharacterInfoControl(_resourceCache);
        }

        public void Opened()
        {
            SendNetworkMessage(new RequestCharacterInfoMessage());
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if(session?.AttachedEntity != Owner) return;

            switch (message)
            {
                case CharacterInfoMessage characterInfoMessage:
                    _control.UpdateUI(characterInfoMessage);
                    if (Owner.TryGetComponent(out ISpriteComponent? spriteComponent))
                    {
                        _control.SpriteView.Sprite = spriteComponent;
                    }

                    _control.NameLabel.Text = Owner.Name;
                    break;
            }
        }

        private sealed class CharacterInfoControl : VBoxContainer
        {
            public SpriteView SpriteView { get; }
            public Label NameLabel { get; }
            public Label SubText { get; }

            public VBoxContainer ObjectivesContainer { get; }

            public CharacterInfoControl(IResourceCache resourceCache)
            {
                IoCManager.InjectDependencies(this);

                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        (SpriteView = new SpriteView { Scale = (2, 2)}),
                        new VBoxContainer
                        {
                            SizeFlagsVertical = SizeFlags.None,
                            Children =
                            {
                                (NameLabel = new Label()),
                                (SubText = new Label
                                {
                                    SizeFlagsVertical = SizeFlags.None,
                                    StyleClasses = {StyleNano.StyleClassLabelSubText},

                                })
                            }
                        }
                    }
                });

                AddChild(new Placeholder(resourceCache)
                {
                    PlaceholderText = Loc.GetString("Health & status effects")
                });

                AddChild(new Label
                {
                    Text = Loc.GetString("Objectives"),
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                });
                ObjectivesContainer = new VBoxContainer();
                AddChild(ObjectivesContainer);

                AddChild(new Placeholder(resourceCache)
                {
                    PlaceholderText = Loc.GetString("Antagonist Roles")
                });
            }

            public void UpdateUI(CharacterInfoMessage characterInfoMessage)
            {
                SubText.Text = characterInfoMessage.JobTitle;

                ObjectivesContainer.RemoveAllChildren();
                foreach (var (groupId, objectiveConditions) in characterInfoMessage.Objectives)
                {
                    var vbox = new VBoxContainer
                    {
                        Modulate = Color.Gray
                    };

                    vbox.AddChild(new Label
                    {
                        Text = groupId,
                        Modulate = Color.LightSkyBlue
                    });

                    foreach (var objectiveCondition in objectiveConditions)
                    {
                        var hbox = new HBoxContainer();
                        hbox.AddChild(new ProgressTextureRect
                        {
                            Texture = objectiveCondition.SpriteSpecifier.Frame0(),
                            Progress = objectiveCondition.Progress,
                            SizeFlagsVertical = SizeFlags.ShrinkCenter
                        });
                        hbox.AddChild(new Control
                        {
                            CustomMinimumSize = (10,0)
                        });
                        hbox.AddChild(new VBoxContainer
                            {
                                Children =
                                {
                                    new Label{Text = objectiveCondition.Title},
                                    new Label{Text = objectiveCondition.Description}
                                }
                            }
                        );
                        vbox.AddChild(hbox);
                    }
                    ObjectivesContainer.AddChild(vbox);
                }
            }
        }
    }
}
