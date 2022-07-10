using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.CharacterAppearance;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Markings;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Species;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Range = Robust.Client.UserInterface.Controls.Range;

namespace Content.Client.Preferences.UI
{
    public sealed class HighlightedContainer : PanelContainer
    {
        public HighlightedContainer()
        {
            PanelOverride = new StyleBoxFlat()
            {
                BackgroundColor = new Color(47, 47, 53),
                ContentMarginTopOverride = 10,
                ContentMarginBottomOverride = 10,
                ContentMarginLeftOverride = 10,
                ContentMarginRightOverride = 10
            };
        }
    }

    [GenerateTypedNameReferences]
    public sealed partial class HumanoidProfileEditor : Control
    {
        private readonly IClientPreferencesManager _preferencesManager;
        private readonly IEntityManager _entMan;
        private readonly IConfigurationManager _configurationManager;

        private LineEdit _ageEdit => CAgeEdit;
        private LineEdit _nameEdit => CNameEdit;
        private LineEdit _flavorTextEdit = null!;
        private Button _nameRandomButton => CNameRandomize;
        private Button _randomizeEverythingButton => CRandomizeEverything;
        private RichTextLabel _warningLabel => CWarningLabel;
        private Button _saveButton => CSaveButton;
        private Button _sexFemaleButton => CSexFemale;
        private Button _sexMaleButton => CSexMale;
        private OptionButton _genderButton => CPronounsButton;
        private Slider _skinColor => CSkin;
        private OptionButton _clothingButton => CClothingButton;
        private OptionButton _backpackButton => CBackpackButton;
        private HairStylePicker _hairPicker => CHairStylePicker;
        private HairStylePicker _facialHairPicker => CFacialHairPicker;
        private EyeColorPicker _eyesPicker => CEyeColorPicker;

        private TabContainer _tabContainer => CTabContainer;
        private BoxContainer _jobList => CJobList;
        private BoxContainer _antagList => CAntagList;
        private readonly List<JobPrioritySelector> _jobPriorities;
        private OptionButton _preferenceUnavailableButton => CPreferenceUnavailableButton;
        private readonly Dictionary<string, BoxContainer> _jobCategories;
        // Mildly hacky, as I don't trust prototype order to stay consistent and don't want the UI to break should a new one get added mid-edit. --moony
        private readonly List<SpeciesPrototype> _speciesList;
        private readonly List<AntagPreferenceSelector> _antagPreferences;

        private Control _previewSpriteControl => CSpriteViewFront;
        private Control _previewSpriteSideControl => CSpriteViewSide;

        private EntityUid? _previewDummy;

        /// <summary>
        /// Used to avoid unnecessarily re-creating the entity.
        /// </summary>
        private string? _lastSpecies;
        private SpriteView? _previewSprite;
        private SpriteView? _previewSpriteSide;

        private BoxContainer _rgbSkinColorContainer => CRgbSkinColorContainer;
        private ColorSelectorSliders _rgbSkinColorSelector;

        private bool _isDirty;
        private bool _needUpdatePreview;
        private bool _needsDummyRebuild;
        public int CharacterSlot;
        public HumanoidCharacterProfile? Profile;

        public event Action<HumanoidCharacterProfile, int>? OnProfileChanged;

        public HumanoidProfileEditor(IClientPreferencesManager preferencesManager, IPrototypeManager prototypeManager,
            IEntityManager entityManager, IConfigurationManager configurationManager)
        {
            RobustXamlLoader.Load(this);
            _random = IoCManager.Resolve<IRobustRandom>();
            _prototypeManager = prototypeManager;
            _entMan = entityManager;
            _preferencesManager = preferencesManager;
            _configurationManager = configurationManager;

            #region Left

            #region Randomize

            #endregion Randomize

            #region Name

            _nameEdit.OnTextChanged += args => { SetName(args.Text); };
            _nameRandomButton.OnPressed += args => RandomizeName();
            _randomizeEverythingButton.OnPressed += args => { RandomizeEverything(); };
            _warningLabel.SetMarkup($"[color=red]{Loc.GetString("humanoid-profile-editor-naming-rules-warning")}[/color]");

            #endregion Name

            #region Appearance

            _tabContainer.SetTabTitle(0, Loc.GetString("humanoid-profile-editor-appearance-tab"));

            #region Sex

            var sexButtonGroup = new ButtonGroup();

            _sexMaleButton.Group = sexButtonGroup;
            _sexMaleButton.OnPressed += args =>
            {
                SetSex(Sex.Male);
                if (Profile?.Gender == Gender.Female)
                {
                    SetGender(Gender.Male);
                    UpdateGenderControls();
                }
            };

            _sexFemaleButton.Group = sexButtonGroup;
            _sexFemaleButton.OnPressed += _ =>
            {
                SetSex(Sex.Female);

                if (Profile?.Gender == Gender.Male)
                {
                    SetGender(Gender.Female);
                    UpdateGenderControls();
                }
            };

            #endregion Sex

            #region Age

            _ageEdit.OnTextChanged += args =>
            {
                if (!int.TryParse(args.Text, out var newAge))
                    return;
                SetAge(newAge);
            };

            #endregion Age

            #region Gender

            _genderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-male-text"), (int) Gender.Male);
            _genderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-female-text"), (int) Gender.Female);
            _genderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-epicene-text"), (int) Gender.Epicene);
            _genderButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-neuter-text"), (int) Gender.Neuter);

            _genderButton.OnItemSelected += args =>
            {
                _genderButton.SelectId(args.Id);
                SetGender((Gender) args.Id);
            };

            #endregion Gender

            #region Species

            _speciesList = prototypeManager.EnumeratePrototypes<SpeciesPrototype>().Where(o => o.RoundStart).ToList();
            for (var i = 0; i < _speciesList.Count; i++)
            {
                var name = Loc.GetString(_speciesList[i].Name);
                CSpeciesButton.AddItem(name, i);
            }

            CSpeciesButton.OnItemSelected += args =>
            {
                CSpeciesButton.SelectId(args.Id);
                SetSpecies(_speciesList[args.Id].ID);
                OnSkinColorOnValueChanged();
            };

            #endregion Species

            #region Skin

            // 0 - 100, 0 being gold/yellowish and 100 being dark
            // HSV based
            //
            // 0 - 20 changes the hue
            // 20 - 100 changes the value
            // 0 is 45 - 20 - 100
            // 20 is 25 - 20 - 100
            // 100 is 25 - 100 - 20
            _skinColor.OnValueChanged += _ =>
            {
                OnSkinColorOnValueChanged();
            };

            _rgbSkinColorContainer.AddChild(_rgbSkinColorSelector = new ColorSelectorSliders());
            _rgbSkinColorSelector.OnColorChanged += _ =>
            {
                OnSkinColorOnValueChanged();
            };

            #endregion

            #region Hair

            _hairPicker.Populate();

            _hairPicker.OnHairStylePicked += newStyle =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithHairStyleName(newStyle));
                IsDirty = true;
            };

            _hairPicker.OnHairColorPicked += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithHairColor(newColor));
                IsDirty = true;
            };

            _facialHairPicker.Populate();

            _facialHairPicker.OnHairStylePicked += newStyle =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithFacialHairStyleName(newStyle));
                IsDirty = true;
            };

            _facialHairPicker.OnHairColorPicked += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithFacialHairColor(newColor));
                IsDirty = true;
            };

            #endregion Hair

            #region Clothing

            _clothingButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-jumpsuit"), (int) ClothingPreference.Jumpsuit);
            _clothingButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-jumpskirt"), (int) ClothingPreference.Jumpskirt);

            _clothingButton.OnItemSelected += args =>
            {
                _clothingButton.SelectId(args.Id);
                SetClothing((ClothingPreference) args.Id);
            };

            #endregion Clothing

            #region Backpack

            _backpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-backpack"), (int) BackpackPreference.Backpack);
            _backpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-satchel"), (int) BackpackPreference.Satchel);
            _backpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-duffelbag"), (int) BackpackPreference.Duffelbag);

            _backpackButton.OnItemSelected += args =>
            {
                _backpackButton.SelectId(args.Id);
                SetBackpack((BackpackPreference) args.Id);
            };

            #endregion Backpack

            #region Eyes

            _eyesPicker.OnEyeColorPicked += newColor =>
            {
                if (Profile is null)
                    return;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithEyeColor(newColor));
                IsDirty = true;
            };

            #endregion Eyes

            #endregion Appearance

            #region Jobs

            _tabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-jobs-tab"));

            _preferenceUnavailableButton.AddItem(
                Loc.GetString("humanoid-profile-editor-preference-unavailable-stay-in-lobby-button"),
                (int) PreferenceUnavailableMode.StayInLobby);
            _preferenceUnavailableButton.AddItem(
                Loc.GetString("humanoid-profile-editor-preference-unavailable-spawn-as-overflow-button",
                              ("overflowJob", Loc.GetString(SharedGameTicker.FallbackOverflowJobName))),
                (int) PreferenceUnavailableMode.SpawnAsOverflow);

            _preferenceUnavailableButton.OnItemSelected += args =>
            {
                _preferenceUnavailableButton.SelectId(args.Id);

                Profile = Profile?.WithPreferenceUnavailable((PreferenceUnavailableMode) args.Id);
                IsDirty = true;
            };

            _jobPriorities = new List<JobPrioritySelector>();
            _jobCategories = new Dictionary<string, BoxContainer>();
            var spriteSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();

            var firstCategory = true;

            foreach (var job in prototypeManager.EnumeratePrototypes<JobPrototype>().OrderBy(j => j.LocalizedName))
            {
                if(!job.SetPreference) { continue; }

                foreach (var department in job.Departments)
                {
                    if (!_jobCategories.TryGetValue(department, out var category))
                    {
                        category = new BoxContainer
                        {
                            Orientation = LayoutOrientation.Vertical,
                            Name = department,
                            ToolTip = Loc.GetString("humanoid-profile-editor-jobs-amount-in-department-tooltip",
                                                    ("departmentName", department))
                        };

                            if (firstCategory)
                            {
                                firstCategory = false;
                            }
                            else
                            {
                                category.AddChild(new Control
                                {
                                    MinSize = new Vector2(0, 23),
                                });
                            }

                        category.AddChild(new PanelContainer
                        {
                            PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#464966")},
                            Children =
                            {
                                new Label
                                {
                                    Text = Loc.GetString("humanoid-profile-editor-department-jobs-label",
                                                         ("departmentName" ,department))
                                }
                            }
                        });

                        _jobCategories[department] = category;
                        _jobList.AddChild(category);
                    }

                    var selector = new JobPrioritySelector(job, spriteSystem);
                    category.AddChild(selector);
                    _jobPriorities.Add(selector);

                    selector.PriorityChanged += priority =>
                    {
                        Profile = Profile?.WithJobPriority(job.ID, priority);
                        IsDirty = true;

                        foreach (var jobSelector in _jobPriorities)
                        {
                            // Sync other selectors with the same job in case of multiple department jobs
                            if (jobSelector.Job == selector.Job)
                            {
                                jobSelector.Priority = priority;
                            }

                            // Lower any other high priorities to medium.
                            if (priority == JobPriority.High)
                            {
                                if (jobSelector.Job != selector.Job && jobSelector.Priority == JobPriority.High)
                                {
                                    jobSelector.Priority = JobPriority.Medium;
                                    Profile = Profile?.WithJobPriority(jobSelector.Job.ID, JobPriority.Medium);
                                }
                            }
                        }
                    };
                }
            }

            #endregion Jobs

            #region Antags

            _tabContainer.SetTabTitle(2, Loc.GetString("humanoid-profile-editor-antags-tab"));

            _antagPreferences = new List<AntagPreferenceSelector>();

            foreach (var antag in prototypeManager.EnumeratePrototypes<AntagPrototype>().OrderBy(a => a.Name))
            {
                if (!antag.SetPreference)
                {
                    continue;
                }

                var selector = new AntagPreferenceSelector(antag);
                _antagList.AddChild(selector);
                _antagPreferences.Add(selector);

                selector.PreferenceChanged += preference =>
                {
                    Profile = Profile?.WithAntagPreference(antag.ID, preference);
                    IsDirty = true;
                };
            }

            #endregion Antags

            #region Save

            _saveButton.OnPressed += args => { Save(); };

            #endregion Save

            #region Markings
            _tabContainer.SetTabTitle(3, Loc.GetString("humanoid-profile-editor-markings-tab"));

            CMarkings.OnMarkingAdded += OnMarkingChange;
            CMarkings.OnMarkingRemoved += OnMarkingChange;
            CMarkings.OnMarkingColorChange += OnMarkingChange;
            CMarkings.OnMarkingRankChange += OnMarkingChange;

            #endregion Markings

            #region FlavorText

            if (_configurationManager.GetCVar(CCVars.FlavorText))
            {
                var flavorText = new FlavorText.FlavorText();
                _tabContainer.AddChild(flavorText);
                _tabContainer.SetTabTitle(_tabContainer.ChildCount-1, Loc.GetString("humanoid-profile-editor-flavortext-tab"));
                _flavorTextEdit = flavorText.CFlavorTextInput;

                flavorText.OnFlavorTextChanged += OnFlavorTextChange;
            }

            #endregion FlavorText

            #endregion Left

            if (preferencesManager.ServerDataLoaded)
            {
                LoadServerData();
            }

            preferencesManager.OnServerDataLoaded += LoadServerData;


            IsDirty = false;
        }

        private void OnFlavorTextChange(string content)
        {
            if (Profile is null)
                return;

            Profile = Profile.WithFlavorText(content);
            IsDirty = true;
        }

        private void OnMarkingChange(MarkingsSet markings)
        {
            if (Profile is null)
                return;

            Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(markings));
            NeedsDummyRebuild = true;
            IsDirty = true;
        }

        private void OnMarkingColorChange(MarkingsSet markings)
        {
            if (Profile is null)
                return;

            Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(markings));
            IsDirty = true;
        }


        private void OnSkinColorOnValueChanged()
        {
            if (Profile is null) return;

            var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;

            switch (skin)
            {
                case SpeciesSkinColor.HumanToned:
                {
                    var range = _skinColor.Value;
                    if (!_skinColor.Visible)
                    {
                        _skinColor.Visible = true;
                        _rgbSkinColorContainer.Visible = false;
                    }

                    var rangeOffset = (int) range - 20;

                    float hue = 25;
                    float sat = 20;
                    float val = 100;

                    if (rangeOffset <= 0)
                    {
                        hue += Math.Abs(rangeOffset);
                    }
                    else
                    {
                        sat += rangeOffset;
                        val -= rangeOffset;
                    }

                    var color = Color.FromHsv(new Vector4(hue / 360, sat / 100, val / 100, 1.0f));

                    CMarkings.CurrentSkinColor = color;
                    Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));
                    break;
                }
                case SpeciesSkinColor.Hues:
                {
                    if (!_rgbSkinColorContainer.Visible)
                    {
                        _skinColor.Visible = false;
                        _rgbSkinColorContainer.Visible = true;
                    }

                    var color = new Color(_rgbSkinColorSelector.Color.R, _rgbSkinColorSelector.Color.G, _rgbSkinColorSelector.Color.B);

                    CMarkings.CurrentSkinColor = color;
                    Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));
                    break;
                }
                case SpeciesSkinColor.TintedHues:
                {
                    if (!_rgbSkinColorContainer.Visible)
                    {
                        _skinColor.Visible = false;
                        _rgbSkinColorContainer.Visible = true;
                    }

                    // a little hacky in order to convert rgb --> hsv --> rgb
                    var color = new Color(_rgbSkinColorSelector.Color.R, _rgbSkinColorSelector.Color.G, _rgbSkinColorSelector.Color.B);
                    var newColor = Color.ToHsv(color);
                    newColor.Y = .1f;
                    color = Color.FromHsv(newColor);

                    CMarkings.CurrentSkinColor = color;
                    Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));
                    break;
                }
            }

            IsDirty = true;
            NeedsDummyRebuild = true; // TODO: ugh - fix this asap
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_previewDummy != null)
                _entMan.DeleteEntity(_previewDummy.Value);

            _preferencesManager.OnServerDataLoaded -= LoadServerData;
        }

        private void RebuildSpriteView()
        {
            var species = Profile?.Species ?? SpeciesManager.DefaultSpecies;
            var dollProto = _prototypeManager.Index<SpeciesPrototype>(species).DollPrototype;

            if (_previewDummy != null)
                _entMan.DeleteEntity(_previewDummy!.Value);

            _previewDummy = _entMan.SpawnEntity(dollProto, MapCoordinates.Nullspace);
            _lastSpecies = species;
            var sprite = _entMan.GetComponent<SpriteComponent>(_previewDummy!.Value);

            if (_previewSprite == null)
            {
                // Front
                _previewSprite = new SpriteView
                {
                    Sprite = sprite,
                    Scale = (6, 6),
                    OverrideDirection = Direction.South,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1
                };
                _previewSpriteControl.AddChild(_previewSprite);
            }
            else
            {
                _previewSprite.Sprite = sprite;
            }

            if (_previewSpriteSide == null)
            {
                _previewSpriteSide = new SpriteView
                {
                    Sprite = sprite,
                    Scale = (6, 6),
                    OverrideDirection = Direction.East,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1
                };
                _previewSpriteSideControl.AddChild(_previewSpriteSide);
            }
            else
            {
                _previewSpriteSide.Sprite = sprite;
            }
        }

        private void LoadServerData()
        {
            Profile = (HumanoidCharacterProfile) _preferencesManager.Preferences!.SelectedCharacter;
            CharacterSlot = _preferencesManager.Preferences.SelectedCharacterIndex;

            NeedsDummyRebuild = true;
            UpdateControls();
        }

        private void SetAge(int newAge)
        {
            Profile = Profile?.WithAge(newAge);
            IsDirty = true;
        }

        private void SetSex(Sex newSex)
        {
            Profile = Profile?.WithSex(newSex);
            IsDirty = true;
        }

        private void SetGender(Gender newGender)
        {
            Profile = Profile?.WithGender(newGender);
            IsDirty = true;
        }

        private void SetSpecies(string newSpecies)
        {
            Profile = Profile?.WithSpecies(newSpecies);
            OnSkinColorOnValueChanged(); // Species may have special color prefs, make sure to update it.
            CMarkings.SetSpecies(newSpecies); // Repopulate the markings tab as well.
            NeedsDummyRebuild = true;
            IsDirty = true;
        }

        private void SetName(string newName)
        {
            Profile = Profile?.WithName(newName);
            IsDirty = true;
        }

        private void SetClothing(ClothingPreference newClothing)
        {
            Profile = Profile?.WithClothingPreference(newClothing);
            IsDirty = true;
        }

        private void SetBackpack(BackpackPreference newBackpack)
        {
            Profile = Profile?.WithBackpackPreference(newBackpack);
            IsDirty = true;
        }

        public void Save()
        {
            IsDirty = false;

            if (Profile != null)
            {
                _preferencesManager.UpdateCharacter(Profile, CharacterSlot);
                NeedsDummyRebuild = true;
                OnProfileChanged?.Invoke(Profile, CharacterSlot);
            }
        }

        private bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                _needUpdatePreview = true;
                UpdateSaveButton();
            }
        }

        private bool NeedsDummyRebuild
        {
            get => _needsDummyRebuild;
            set
            {
                _needsDummyRebuild = value;
                _needUpdatePreview = true;
            }
        }

        private void UpdateNameEdit()
        {
            _nameEdit.Text = Profile?.Name ?? "";
        }

        private void UpdateFlavorTextEdit()
        {
            if(_flavorTextEdit != null)
            {
                _flavorTextEdit.Text = Profile?.FlavorText ?? "";
            }
        }

        private void UpdateAgeEdit()
        {
            _ageEdit.Text = Profile?.Age.ToString() ?? "";
        }

        private void UpdateSexControls()
        {
            if (Profile?.Sex == Sex.Male)
                _sexMaleButton.Pressed = true;
            else
                _sexFemaleButton.Pressed = true;
        }

        private void UpdateSkinColor()
        {
            if (Profile == null)
                return;

            var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;

            switch (skin)
            {
                case SpeciesSkinColor.HumanToned:
                {
                    if (!_skinColor.Visible)
                    {
                        _skinColor.Visible = true;
                        _rgbSkinColorContainer.Visible = false;
                    }

                    var color = Color.ToHsv(Profile.Appearance.SkinColor);
                    // check for hue/value first, if hue is lower than this percentage
                    // and value is 1.0
                    // then it'll be hue
                    if (Math.Clamp(color.X, 25f / 360f, 1) > 25f / 360f
                        && color.Z == 1.0)
                    {
                        _skinColor.Value = Math.Abs(45 - (color.X * 360));
                    }
                    // otherwise it'll directly be the saturation
                    else
                    {
                        _skinColor.Value = color.Y * 100;
                    }
                    break;
                }
                case SpeciesSkinColor.Hues:
                {
                    if (!_rgbSkinColorContainer.Visible)
                    {
                        _skinColor.Visible = false;
                        _rgbSkinColorContainer.Visible = true;
                    }

                    // set the RGB values to the direct values otherwise
                    _rgbSkinColorSelector.Color = Profile.Appearance.SkinColor;
                    break;
                }
                case SpeciesSkinColor.TintedHues:
                {
                    if (!_rgbSkinColorContainer.Visible)
                    {
                        _skinColor.Visible = false;
                        _rgbSkinColorContainer.Visible = true;
                    }

                    // set the RGB values to the direct values otherwise
                    _rgbSkinColorSelector.Color = Profile.Appearance.SkinColor;
                    break;
                }
            }

        }

        private void UpdateMarkings()
        {
            if (Profile == null)
            {
                return;
            }

            CMarkings.SetData(Profile.Appearance.Markings, Profile.Species, Profile.Appearance.SkinColor);
        }

        private void UpdateSpecies()
        {
            if (Profile == null)
            {
                return;
            }

            CSpeciesButton.Select(_speciesList.FindIndex(x => x.ID == Profile.Species));
        }

        private void UpdateGenderControls()
        {
            if (Profile == null)
            {
                return;
            }

            _genderButton.SelectId((int) Profile.Gender);
        }

        private void UpdateClothingControls()
        {
            if (Profile == null)
            {
                return;
            }

            _clothingButton.SelectId((int) Profile.Clothing);
        }

        private void UpdateBackpackControls()
        {
            if (Profile == null)
            {
                return;
            }

            _backpackButton.SelectId((int) Profile.Backpack);
        }

        private void UpdateHairPickers()
        {
            if (Profile == null)
            {
                return;
            }

            _hairPicker.SetData(
                Profile.Appearance.HairColor,
                Profile.Appearance.HairStyleId,
                SpriteAccessoryCategories.HumanHair,
                true);
            _facialHairPicker.SetData(
                Profile.Appearance.FacialHairColor,
                Profile.Appearance.FacialHairStyleId,
                SpriteAccessoryCategories.HumanFacialHair,
                true);
        }

        private void UpdateEyePickers()
        {
            if (Profile == null)
            {
                return;
            }

            _eyesPicker.SetData(Profile.Appearance.EyeColor);
        }

        private void UpdateSaveButton()
        {
            _saveButton.Disabled = Profile is null || !IsDirty;
        }

        private void UpdatePreview()
        {
            if (Profile is null)
                return;

            if (_needsDummyRebuild)
            {
                RebuildSpriteView(); // Species change also requires sprite rebuild, so we'll do that now.
                _needsDummyRebuild = false;
            }

            EntitySystem.Get<SharedHumanoidAppearanceSystem>().UpdateFromProfile(_previewDummy!.Value, Profile);
            LobbyCharacterPreviewPanel.GiveDummyJobClothes(_previewDummy!.Value, Profile);
        }

        public void UpdateControls()
        {
            if (Profile is null) return;
            UpdateNameEdit();
            UpdateFlavorTextEdit();
            UpdateSexControls();
            UpdateGenderControls();
            UpdateSkinColor();
            UpdateSpecies();
            UpdateClothingControls();
            UpdateBackpackControls();
            UpdateAgeEdit();
            UpdateHairPickers();
            UpdateEyePickers();
            UpdateSaveButton();
            UpdateJobPriorities();
            UpdateAntagPreferences();
            UpdateMarkings();

            NeedsDummyRebuild = true;

            _preferenceUnavailableButton.SelectId((int) Profile.PreferenceUnavailable);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_needUpdatePreview)
            {
                UpdatePreview();

                _needUpdatePreview = false;
            }
        }

        private void UpdateJobPriorities()
        {
            foreach (var prioritySelector in _jobPriorities)
            {
                var jobId = prioritySelector.Job.ID;

                var priority = Profile?.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never) ?? JobPriority.Never;

                prioritySelector.Priority = priority;
            }
        }

        private sealed class JobPrioritySelector : Control
        {
            public JobPrototype Job { get; }
            private readonly RadioOptions<int> _optionButton;

            public JobPriority Priority
            {
                get => (JobPriority) _optionButton.SelectedValue;
                set => _optionButton.SelectByValue((int) value);
            }

            public event Action<JobPriority>? PriorityChanged;

            public JobPrioritySelector(JobPrototype job, SpriteSystem sprites)
            {
                Job = job;

                _optionButton = new RadioOptions<int>(RadioOptionsLayout.Horizontal)
                {
                    FirstButtonStyle = StyleBase.ButtonOpenRight,
                    ButtonStyle = StyleBase.ButtonOpenBoth,
                    LastButtonStyle = StyleBase.ButtonOpenLeft
                };

                // Text, Value
                _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-high-button"), (int) JobPriority.High);
                _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-medium-button"), (int) JobPriority.Medium);
                _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-low-button"), (int) JobPriority.Low);
                _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-never-button"), (int) JobPriority.Never);

                _optionButton.OnItemSelected += args =>
                {
                    _optionButton.Select(args.Id);
                    PriorityChanged?.Invoke(Priority);
                };

                var icon = new TextureRect
                {
                    TextureScale = (2, 2),
                    Stretch = TextureRect.StretchMode.KeepCentered
                };

                var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/job_icons.rsi"),
                    job.Icon);
                icon.Texture = sprites.Frame0(specifier);

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        icon,
                        new Label {Text = job.LocalizedName, MinSize = (175, 0)},
                        _optionButton
                    }
                });
            }
        }

        private void UpdateAntagPreferences()
        {
            foreach (var preferenceSelector in _antagPreferences)
            {
                var antagId = preferenceSelector.Antag.ID;
                var preference = Profile?.AntagPreferences.Contains(antagId) ?? false;

                preferenceSelector.Preference = preference;
            }
        }

        private sealed class AntagPreferenceSelector : Control
        {
            public AntagPrototype Antag { get; }
            private readonly CheckBox _checkBox;

            public bool Preference
            {
                get => _checkBox.Pressed;
                set => _checkBox.Pressed = value;
            }

            public event Action<bool>? PreferenceChanged;

            public AntagPreferenceSelector(AntagPrototype antag)
            {
                Antag = antag;

                _checkBox = new CheckBox {Text = $"{antag.Name}"};
                _checkBox.OnToggled += OnCheckBoxToggled;

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        _checkBox
                    }
                });
            }

            private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
            {
                PreferenceChanged?.Invoke(Preference);
            }
        }
    }
}
