﻿#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum ExpendableLightVisuals
    {
        State
    }

    [Serializable, NetSerializable]
    public enum ExpendableLightState
    {
        BrandNew,
        Lit,
        Fading,
        Dead
    }

    public abstract class SharedExpendableLightComponent: Component
    {
        public sealed override string Name => "ExpendableLight";

        [ViewVariables(VVAccess.ReadOnly)]
        protected ExpendableLightState CurrentState { get; set; }

        [ViewVariables]
        [DataField("turnOnBehaviourID")]
        protected string TurnOnBehaviourID { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("fadeOutBehaviourID")]
        protected string FadeOutBehaviourID { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("glowDuration")]
        protected float GlowDuration { get; set; } = 60 * 15f;

        [ViewVariables]
        [DataField("fadeOutDuration")]
        protected float FadeOutDuration { get; set; } = 60 * 5f;

        [ViewVariables]
        [DataField("spentDesc")]
        protected string SpentDesc { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("spentName")]
        protected string SpentName { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("iconStateSpent")]
        protected string IconStateSpent { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("iconStateOn")]
        protected string IconStateLit { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("litSound")]
        protected string LitSound { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("loopedSound")]
        protected string LoopedSound { get; set; } = string.Empty;

        [ViewVariables]
        [DataField("dieSound")]
        protected string DieSound { get; set; } = string.Empty;
    }
}
