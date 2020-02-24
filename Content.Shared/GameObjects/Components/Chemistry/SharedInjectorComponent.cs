﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Shared class for injectors & syringes
    /// </summary>
    public class SharedInjectorComponent : Component
    {
        public override string Name => "Injector";
        public sealed override uint? NetID => ContentNetIDs.REAGENT_INJECTOR;

        /// <summary>
        /// Component data used for net updates. Used by client for item status ui
        /// </summary>
        [Serializable, NetSerializable]
        protected sealed class InjectorComponentState : ComponentState
        {
            public int CurrentVolume { get; }
            public int TotalVolume { get; }
            public InjectorToggleMode CurrentMode { get; }

            public InjectorComponentState(int currentVolume, int totalVolume, InjectorToggleMode currentMode) : base(ContentNetIDs.REAGENT_INJECTOR)
            {
                CurrentVolume = currentVolume;
                TotalVolume = totalVolume;
                CurrentMode = currentMode;
            }
        }

        protected enum InjectorToggleMode
        {
            Inject,
            Draw
        }
    }
}
