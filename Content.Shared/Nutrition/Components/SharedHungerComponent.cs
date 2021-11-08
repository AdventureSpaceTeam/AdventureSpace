using System;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Nutrition.Components
{
    [NetworkedComponent()]
    public abstract class SharedHungerComponent : Component
    {
        public sealed override string Name => "Hunger";

        [ViewVariables]
        public abstract HungerThreshold CurrentHungerThreshold { get; }

        [Serializable, NetSerializable]
        protected sealed class HungerComponentState : ComponentState
        {
            public HungerThreshold CurrentThreshold { get; }

            public HungerComponentState(HungerThreshold currentThreshold)
            {
                CurrentThreshold = currentThreshold;
            }
        }
    }

    [Serializable, NetSerializable]
    public enum HungerThreshold : byte
    {
        Overfed,
        Okay,
        Peckish,
        Starving,
        Dead,
    }
}
