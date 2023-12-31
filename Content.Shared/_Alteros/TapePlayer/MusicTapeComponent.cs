using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.TapePlayer
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MusicTapeComponent : Component
    {
        [DataField("sound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
        public SoundSpecifier? Sound;
    }
}
