﻿using Content.Shared.Preferences;
using Content.Shared.Prototypes;
using Content.Shared.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.UserInterface
{
    public partial class HumanoidProfileEditor
    {
        private readonly IRobustRandom _random;
        private readonly IPrototypeManager _prototypeManager;

        private void RandomizeEverything()
        {
            Profile = HumanoidCharacterProfile.Random();
            UpdateSexControls();
            UpdateGenderControls();
            UpdateClothingControls();
            UpdateAgeEdit();
            UpdateNameEdit();
            UpdateHairPickers();
        }

        private void RandomizeName()
        {
            var firstName = _random.Pick(Profile.Sex.FirstNames(_prototypeManager).Values);
            var lastName = _random.Pick(_prototypeManager.Index<DatasetPrototype>("names_last"));
            SetName($"{firstName} {lastName}");
            UpdateNameEdit();
        }
    }
}
