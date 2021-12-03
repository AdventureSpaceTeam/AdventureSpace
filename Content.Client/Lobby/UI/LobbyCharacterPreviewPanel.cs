using System.Linq;
using Content.Client.HUD.UI;
using Content.Client.Inventory;
using Content.Client.Preferences;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.Inventory.EquipmentSlotDefines;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI
{
    public class LobbyCharacterPreviewPanel : Control
    {
        private readonly IClientPreferencesManager _preferencesManager;
        private IEntity _previewDummy;
        private readonly Label _summaryLabel;
        private readonly BoxContainer _loaded;
        private readonly Label _unloaded;

        public LobbyCharacterPreviewPanel(IEntityManager entityManager,
            IClientPreferencesManager preferencesManager)
        {
            _preferencesManager = preferencesManager;
            _previewDummy = entityManager.SpawnEntity("MobHumanDummy", MapCoordinates.Nullspace);

            var header = new NanoHeading
            {
                Text = Loc.GetString("lobby-character-preview-panel-header")
            };

            CharacterSetupButton = new Button
            {
                Text = Loc.GetString("lobby-character-preview-panel-character-setup-button"),
                HorizontalAlignment = HAlignment.Left
            };

            _summaryLabel = new Label();

            var viewSouth = MakeSpriteView(_previewDummy, Direction.South);
            var viewNorth = MakeSpriteView(_previewDummy, Direction.North);
            var viewWest = MakeSpriteView(_previewDummy, Direction.West);
            var viewEast = MakeSpriteView(_previewDummy, Direction.East);

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            vBox.AddChild(header);

            _unloaded = new Label {Text = Loc.GetString("lobby-character-preview-panel-unloaded-preferences-label")};

            _loaded = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Visible = false
            };

            _loaded.AddChild(CharacterSetupButton);
            _loaded.AddChild(_summaryLabel);

            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            hBox.AddChild(viewSouth);
            hBox.AddChild(viewNorth);
            hBox.AddChild(viewWest);
            hBox.AddChild(viewEast);

            _loaded.AddChild(hBox);

            vBox.AddChild(_loaded);
            vBox.AddChild(_unloaded);
            AddChild(vBox);

            UpdateUI();

            _preferencesManager.OnServerDataLoaded += UpdateUI;
        }

        public Button CharacterSetupButton { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _preferencesManager.OnServerDataLoaded -= UpdateUI;

            if (!disposing) return;
            IoCManager.Resolve<IEntityManager>().DeleteEntity(_previewDummy.Uid);
            _previewDummy = null!;
        }

        private static SpriteView MakeSpriteView(IEntity entity, Direction direction)
        {
            return new()
            {
                Sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity.Uid),
                OverrideDirection = direction,
                Scale = (2, 2)
            };
        }

        public void UpdateUI()
        {
            if (!_preferencesManager.ServerDataLoaded)
            {
                _loaded.Visible = false;
                _unloaded.Visible = true;
            }
            else
            {
                _loaded.Visible = true;
                _unloaded.Visible = false;
                if (_preferencesManager.Preferences?.SelectedCharacter is not HumanoidCharacterProfile selectedCharacter)
                {
                    _summaryLabel.Text = string.Empty;
                }
                else
                {
                    _summaryLabel.Text = selectedCharacter.Summary;
                    EntitySystem.Get<SharedHumanoidAppearanceSystem>().UpdateFromProfile(_previewDummy.Uid, selectedCharacter);
                    GiveDummyJobClothes(_previewDummy, selectedCharacter);
                }
            }
        }

        public static void GiveDummyJobClothes(IEntity dummy, HumanoidCharacterProfile profile)
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();

            var inventory = IoCManager.Resolve<IEntityManager>().GetComponent<ClientInventoryComponent>(dummy.Uid);

            var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;

            var job = protoMan.Index<JobPrototype>(highPriorityJob ?? SharedGameTicker.FallbackOverflowJob);

            inventory.ClearAllSlotVisuals();

            if (job.StartingGear != null)
            {
                var entityMan = IoCManager.Resolve<IEntityManager>();
                var gear = protoMan.Index<StartingGearPrototype>(job.StartingGear);

                foreach (var slot in AllSlots)
                {
                    var itemType = gear.GetGear(slot, profile);
                    if (itemType != string.Empty)
                    {
                        var item = entityMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                        inventory.SetSlotVisuals(slot, item);
                        IoCManager.Resolve<IEntityManager>().DeleteEntity(item.Uid);
                    }
                }
            }
        }
    }
}
