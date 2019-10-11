﻿using System.Collections.Generic;
using Content.Shared.Interfaces;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Chemistry
{
    /// <summary>
    /// Prototype for chemical reaction definitions
    /// </summary>
    [Prototype("reaction")]
    public class ReactionPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private Dictionary<string, ReactantPrototype> _reactants;
        private Dictionary<string, uint> _products;
        private List<IReactionEffect> _effects;

        public string ID => _id;
        public string Name => _name;
        /// <summary>
        /// Reactants required for the reaction to occur.
        /// </summary>
        public IReadOnlyDictionary<string, ReactantPrototype> Reactants => _reactants;
        /// <summary>
        /// Reagents created when the reaction occurs.
        /// </summary>
        public IReadOnlyDictionary<string, uint> Products => _products;
        /// <summary>
        /// Effects to be triggered when the reaction occurs.
        /// </summary>
        public IReadOnlyList<IReactionEffect> Effects => _effects;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _reactants, "reactants", new Dictionary<string, ReactantPrototype>());
            serializer.DataField(ref _products, "products", new Dictionary<string, uint>());
            serializer.DataField(ref _effects, "effects", new List<IReactionEffect>());
        }
    }

    /// <summary>
    /// Prototype for chemical reaction reactants.
    /// </summary>
    public class ReactantPrototype : IExposeData
    {
        private int _amount;
        private bool _catalyst;

        /// <summary>
        /// Minimum amount of the reactant needed for the reaction to occur.
        /// </summary>
        public int Amount => _amount;
        /// <summary>
        /// Whether or not the reactant is a catalyst. Catalysts aren't removed when a reaction occurs.
        /// </summary>
        public bool Catalyst => _catalyst;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _amount, "amount", 1);
            serializer.DataField(ref _catalyst, "catalyst", false);
        }
    }
}
