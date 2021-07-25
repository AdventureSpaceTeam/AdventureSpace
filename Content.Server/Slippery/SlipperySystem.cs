using Content.Shared.Audio;
using Content.Shared.Slippery;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Slippery
{
    internal sealed class SlipperySystem : SharedSlipperySystem
    {
        protected override void PlaySound(SlipperyComponent component)
        {
            if (component.SlipSound.TryGetSound(out var slipSound))
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), slipSound, component.Owner, AudioHelpers.WithVariation(0.2f));
            }
        }
    }
}
