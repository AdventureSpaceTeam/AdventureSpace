using Content.Server.Chemistry.Components;
using Content.Server.Fluids.Components;
using Content.Shared.Audio;
using Content.Shared.Sound;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    public class CreamPieComponent : Component, ILand, IThrowCollide
    {
        public override string Name => "CreamPie";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; set; } = 1f;

        [DataField("sound")]
        private SoundSpecifier _sound = new SoundCollectionSpecifier("desacration");

        public void PlaySound()
        {
            SoundSystem.Play(Filter.Pvs(Owner), _sound.GetSound(), Owner, AudioHelpers.WithVariation(0.125f));
        }

        void IThrowCollide.DoHit(ThrowCollideEventArgs eventArgs)
        {
            Splat();
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            Splat();
        }

        public void Splat()
        {
            PlaySound();

            if (Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                solution.Solution.SpillAt(Owner, "PuddleSmear", false);
            }

            Owner.QueueDelete();
        }
    }
}
