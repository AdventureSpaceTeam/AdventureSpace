using System;
using System.Linq;
using Content.Server.Database;
using Content.Shared.Preferences;
using Robust.Shared.Maths;
using static Content.Shared.Preferences.Sex;

namespace Content.Server.Preferences
{
    /// <summary>
    ///     Provides methods to retrieve and update character preferences.
    ///     Don't use this directly, go through <see cref="ServerPreferencesManager" /> instead.
    /// </summary>
    public class PreferencesDatabase
    {
        private readonly int _maxCharacterSlots;
        private readonly PrefsDb _prefsDb;

        public PreferencesDatabase(string databaseFilePath, int maxCharacterSlots)
        {
            _maxCharacterSlots = maxCharacterSlots;
            _prefsDb = new PrefsDb(databaseFilePath);
        }

        public PlayerPreferences GetPlayerPreferences(string username)
        {
            var prefs = _prefsDb.GetPlayerPreferences(username);
            if (prefs is null) return null;

            var profiles = new ICharacterProfile[_maxCharacterSlots];
            foreach (var profile in prefs.HumanoidProfiles)
                profiles[profile.Slot] = new HumanoidCharacterProfile
                {
                    Name = profile.CharacterName,
                    Age = profile.Age,
                    Sex = profile.Sex == "Male" ? Male : Female,
                    CharacterAppearance = new HumanoidCharacterAppearance
                    {
                        HairStyleName = profile.HairName,
                        HairColor = Color.FromHex(profile.HairColor),
                        FacialHairStyleName = profile.FacialHairName,
                        FacialHairColor = Color.FromHex(profile.FacialHairColor),
                        EyeColor = Color.FromHex(profile.EyeColor),
                        SkinColor = Color.FromHex(profile.SkinColor)
                    }
                };

            return new PlayerPreferences
            {
                SelectedCharacterIndex = prefs.SelectedCharacterSlot,
                Characters = profiles.ToList()
            };
        }

        public void SaveSelectedCharacterIndex(string username, int index)
        {
            index = index.Clamp(0, _maxCharacterSlots - 1);
            _prefsDb.SaveSelectedCharacterIndex(username, index);
        }

        public void SaveCharacterSlot(string username, ICharacterProfile profile, int slot)
        {
            if (slot < 0 || slot >= _maxCharacterSlots)
                return;
            if (profile is null)
            {
                DeleteCharacterSlot(username, slot);
                return;
            }

            if (!(profile is HumanoidCharacterProfile humanoid))
                // TODO: Handle other ICharacterProfile implementations properly
                throw new NotImplementedException();

            var appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;
            _prefsDb.SaveCharacterSlot(username, new HumanoidProfile
            {
                CharacterName = humanoid.Name,
                Age = humanoid.Age,
                Sex = humanoid.Sex.ToString(),
                HairName = appearance.HairStyleName,
                HairColor = appearance.HairColor.ToHex(),
                FacialHairName = appearance.FacialHairStyleName,
                FacialHairColor = appearance.FacialHairColor.ToHex(),
                EyeColor = appearance.EyeColor.ToHex(),
                SkinColor = appearance.SkinColor.ToHex(),
                Slot = slot
            });
        }

        private void DeleteCharacterSlot(string username, int slot)
        {
            _prefsDb.DeleteCharacterSlot(username, slot);
        }
    }
}
