using Content.Corvax.Interfaces.Client;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client.Preferences.UI
{
    public sealed partial class HumanoidProfileEditor
    {
        private readonly IPrototypeManager _prototypeManager;

        private void RandomizeEverything()
        {
            var sponsors = IoCManager.Resolve<IClientSponsorsManager>(); // Alteros-Sponsors
            var ignoredSpecies = new HashSet<string>();
            foreach (var speciesPrototype in _prototypeManager.EnumeratePrototypes<SpeciesPrototype>())
            {
                if (speciesPrototype.SponsorOnly && !sponsors.Prototypes.Contains(speciesPrototype.ID))
                    ignoredSpecies.Add(speciesPrototype.ID);
            }
            Profile = HumanoidCharacterProfile.Random(ignoredSpecies);
            UpdateControls();
            IsDirty = true;
        }

        private void RandomizeName()
        {
            if (Profile == null) return;
            var name = HumanoidCharacterProfile.GetName(Profile.Species, Profile.Gender);
            SetName(name);
            UpdateNameEdit();
        }
    }
}
