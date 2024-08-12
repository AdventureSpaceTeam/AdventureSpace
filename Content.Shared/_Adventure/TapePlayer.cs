using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.TapePlayer;

[RegisterComponent, NetworkedComponent]
public sealed partial class TapePlayerComponent : Component
{
    public EntityUid? AudioStream = default;

    public bool Played = false;

    [DataField("tapeSlot", required: true)]
    public ItemSlot TapeSlot = new();

    [DataField("volume")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Volume;

    [DataField("rolloffFactor")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RolloffFactor = 1f;

    [DataField("maxDistance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxDistance = 20f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MusicTapeComponent : Component
{
    [DataField("sound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? Sound;
}
