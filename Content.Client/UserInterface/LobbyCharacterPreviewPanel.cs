﻿using System.Linq;
using Content.Client.GameObjects;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.Interfaces;
using Content.Shared;
using Content.Shared.Jobs;
using Content.Shared.Preferences;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{
    public class LobbyCharacterPreviewPanel : Control
    {
        private readonly IClientPreferencesManager _preferencesManager;
        private IEntity _previewDummy;
        private readonly Label _summaryLabel;

        public LobbyCharacterPreviewPanel(IEntityManager entityManager,
            IClientPreferencesManager preferencesManager)
        {
            _preferencesManager = preferencesManager;
            _previewDummy = entityManager.SpawnEntity("HumanMob_Dummy", MapCoordinates.Nullspace);

            var header = new NanoHeading
            {
                Text = Loc.GetString("Character setup")
            };

            CharacterSetupButton = new Button
            {
                Text = Loc.GetString("Customize"),
                SizeFlagsHorizontal = SizeFlags.None
            };

            _summaryLabel = new Label();

            var viewSouth = MakeSpriteView(_previewDummy, Direction.South);
            var viewNorth = MakeSpriteView(_previewDummy, Direction.North);
            var viewWest = MakeSpriteView(_previewDummy, Direction.West);
            var viewEast = MakeSpriteView(_previewDummy, Direction.East);

            var vBox = new VBoxContainer();

            vBox.AddChild(header);
            vBox.AddChild(CharacterSetupButton);

            vBox.AddChild(_summaryLabel);

            var hBox = new HBoxContainer();
            hBox.AddChild(viewSouth);
            hBox.AddChild(viewNorth);
            hBox.AddChild(viewWest);
            hBox.AddChild(viewEast);

            vBox.AddChild(hBox);

            AddChild(vBox);

            UpdateUI();
        }

        public Button CharacterSetupButton { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _previewDummy.Delete();
            _previewDummy = null;
        }

        private static SpriteView MakeSpriteView(IEntity entity, Direction direction)
        {
            return new SpriteView
            {
                Sprite = entity.GetComponent<ISpriteComponent>(),
                OverrideDirection = direction,
                Scale = (2, 2)
            };
        }

        public void UpdateUI()
        {
            if (!(_preferencesManager.Preferences.SelectedCharacter is HumanoidCharacterProfile selectedCharacter))
            {
                _summaryLabel.Text = string.Empty;
            }
            else
            {
                _summaryLabel.Text = selectedCharacter.Summary;
                var component = _previewDummy.GetComponent<HumanoidAppearanceComponent>();
                component.UpdateFromProfile(selectedCharacter);

                GiveDummyJobClothes(_previewDummy, selectedCharacter);
            }
        }

        public static void GiveDummyJobClothes(IEntity dummy, HumanoidCharacterProfile profile)
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            var entityMan = IoCManager.Resolve<IEntityManager>();

            var inventory = dummy.GetComponent<ClientInventoryComponent>();

            var highPriorityJob = profile.JobPriorities.SingleOrDefault(p => p.Value == JobPriority.High).Key;

            var job = protoMan.Index<JobPrototype>(highPriorityJob ?? SharedGameTicker.OverflowJob);
            var gear = protoMan.Index<StartingGearPrototype>(job.StartingGear);

            inventory.ClearAllSlotVisuals();

            foreach (var (slot, itemType) in gear.Equipment)
            {
                var item = entityMan.SpawnEntity(itemType, MapCoordinates.Nullspace);

                inventory.SetSlotVisuals(slot, item);

                item.Delete();
            }
        }
    }
}
