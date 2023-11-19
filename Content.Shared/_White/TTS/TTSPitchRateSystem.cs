using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Shared.White.TTS;

public sealed class TTSPitchRateSystem : EntitySystem
{

    public bool TryGetPitchRate(EntityUid uid, out List<string> pitchRate)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            pitchRate = new List<string>();
            return false;
        }

        pitchRate = new List<string> {"medium", "medium"};

        GetPitchRateForSpecies(uid, humanoid, ref pitchRate);
        return true;
    }

    private void GetPitchRateForSpecies(EntityUid uid, HumanoidAppearanceComponent humanoid, ref List<string> pitchRate)
    {
        switch (humanoid.Species)
        {
            case "SlimePerson":
                pitchRate[0] = "high";
                pitchRate[1] = "medium";
                break;
            case "Arachnid":
                pitchRate[0] = "x-high";
                pitchRate[1] = "x-fast";
                break;
            case "Human":
                var meta = MetaData(uid);
                if (meta.EntityPrototype != null && meta.EntityPrototype.ToString() == "MobDwarf") //Dwarfs
                {
                    pitchRate[0] = "high";
                    pitchRate[1] = "slow";
                }
                else if (humanoid.SkinColor.R >= 0.6)
                {
                    pitchRate[0] = "x-low";
                    pitchRate[1] = "medium";
                }
                else
                {
                    pitchRate[0] = "medium";
                    pitchRate[1] = "medium";
                }
                break;
            case "Diona":
                pitchRate[0] = "x-low";
                pitchRate[1] = "x-slow";
                break;
            case "Reptilian":
                pitchRate[0] = "low";
                pitchRate[1] = "slow";
                break;
            case "Skrell":
                pitchRate[0] = "medium";
                pitchRate[1] = "medium";
                break;
            case "Skeleton":
                pitchRate[0] = "medium";
                pitchRate[1] = "medium";
                break;
        }
    }
}
