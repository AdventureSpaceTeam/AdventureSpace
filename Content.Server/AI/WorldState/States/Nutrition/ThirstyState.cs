using System;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.Components.Nutrition;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Nutrition
{
    [UsedImplicitly]
    public class ThirstyState : StateData<bool>
    {
        public override string Name => "Thirsty";

        public override bool GetValue()
        {
            if (!Owner.TryGetComponent(out ThirstComponent thirstComponent))
            {
                return false;
            }

            switch (thirstComponent.CurrentThirstThreshold)
            {
                case ThirstThreshold.OverHydrated:
                    return false;
                case ThirstThreshold.Okay:
                    return false;
                case ThirstThreshold.Thirsty:
                    return true;
                case ThirstThreshold.Parched:
                    return true;
                case ThirstThreshold.Dead:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(thirstComponent.CurrentThirstThreshold),
                        thirstComponent.CurrentThirstThreshold,
                        null);
            }
        }
    }
}
