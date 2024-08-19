using Robust.Shared.Serialization;

namespace Content.Shared.AdventureSpace.Medical.Diseases.Effects;

[Serializable, NetSerializable]
public enum DiseaseVisualLayers : byte
{
    Head,
    Torso,
    LArm,
    RArm,
    LLeg,
    RLeg
}

[Serializable, NetSerializable]
public enum DiseaseClearKey : byte
{
    Clear
}

[Serializable, NetSerializable]
public sealed class DiseaseVisualsData(string sprite, string state) : ICloneable
{
    public string Sprite = sprite;
    public string State = state;

    public object Clone()
    {
        return new DiseaseVisualsData(Sprite, State);
    }
}
