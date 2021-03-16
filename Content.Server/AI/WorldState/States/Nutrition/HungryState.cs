using System;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.Components.Nutrition;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Nutrition
{
    [UsedImplicitly]
    public sealed class HungryState : StateData<bool>
    {
        public override string Name => "Hungry";

        public override bool GetValue()
        {
            if (!Owner.TryGetComponent(out HungerComponent? hungerComponent))
            {
                return false;
            }

            switch (hungerComponent.CurrentHungerThreshold)
            {
                case HungerThreshold.Overfed:
                    return false;
                case HungerThreshold.Okay:
                    return false;
                case HungerThreshold.Peckish:
                    return true;
                case HungerThreshold.Starving:
                    return true;
                case HungerThreshold.Dead:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(hungerComponent.CurrentHungerThreshold),
                        hungerComponent.CurrentHungerThreshold,
                        null);
            }
        }
    }
}
