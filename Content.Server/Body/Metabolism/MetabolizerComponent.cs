﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Body.Metabolism
{
    /// <summary>
    ///     Handles metabolizing various reagents with given effects.
    /// </summary>
    [RegisterComponent]
    public class MetabolizerComponent : Component
    {
        public override string Name => "Metabolizer";

        public float AccumulatedFrametime = 0.0f;

        /// <summary>
        ///     How often to metabolize reagents, in seconds.
        /// </summary>
        /// <returns></returns>
        [DataField("updateFrequency")]
        public float UpdateFrequency = 1.0f;

        /// <summary>
        ///     Whether this metabolizer should attempt to metabolize chemicals in its parent bodies' bloodstream,
        ///     as opposed to a solution container on the metabolizing entity itself.
        /// </summary>
        [DataField("takeFromBloodstream")]
        public bool TakeFromBloodstream = true;

        /// <summary>
        ///     A dictionary mapping reagent string IDs to a list of effects & associated metabolism rate.
        /// </summary>
        /// <returns></returns>
        [DataField("metabolisms", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<ReagentEffectsEntry, ReagentPrototype>))]
        public Dictionary<string, ReagentEffectsEntry> Metabolisms = default!;
    }

    [DataDefinition]
    public class ReagentEffectsEntry
    {
        /// <summary>
        ///     Amount of reagent to metabolize, per metabolism cycle.
        /// </summary>
        [DataField("metabolismRate")]
        public ReagentUnit MetabolismRate = ReagentUnit.New(1.0f);

        /// <summary>
        ///     A list of effects to apply when these reagents are metabolized.
        /// </summary>
        [DataField("effects", required: true)]
        public ReagentEffect[] Effects = default!;
    }
}
