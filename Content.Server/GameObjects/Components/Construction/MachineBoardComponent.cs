﻿using System;
using System.Collections.Generic;
using Content.Server.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Stacks;
using Microsoft.Extensions.Logging;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineBoardComponent : Component, IExamine
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "MachineBoard";

        [ViewVariables]
        private Dictionary<MachinePart, int> _requirements;

        [ViewVariables]
        private Dictionary<string, int> _materialIdRequirements;

        [ViewVariables]
        private Dictionary<string, ComponentPartInfo> _componentRequirements;

        [ViewVariables(VVAccess.ReadWrite)]
        public string Prototype { get; private set; }
        public IReadOnlyDictionary<MachinePart, int> Requirements => _requirements;
        public IReadOnlyDictionary<string, int> MaterialIdRequirements => _materialIdRequirements;

        public IEnumerable<KeyValuePair<StackPrototype, int>> MaterialRequirements
        {
            get
            {
                foreach (var (materialId, amount) in MaterialIdRequirements)
                {
                    var material = _prototypeManager.Index<StackPrototype>(materialId);
                    yield return new KeyValuePair<StackPrototype, int>(material, amount);
                }
            }
        }

        public IReadOnlyDictionary<string, ComponentPartInfo> ComponentRequirements => _componentRequirements;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.Prototype, "prototype", null);
            serializer.DataField(ref _requirements, "requirements", new Dictionary<MachinePart, int>());
            serializer.DataField(ref _materialIdRequirements, "materialRequirements", new Dictionary<string, int>());
            serializer.DataField(ref _componentRequirements, "componentRequirements", new Dictionary<string, ComponentPartInfo>());
        }

        protected override void Startup()
        {
            base.Startup();

            foreach (var material in _materialIdRequirements.Keys)
            {
                if (!_prototypeManager.HasIndex<StackPrototype>(material))
                {
                    Logger.Error($"No {nameof(StackPrototype)} found with id {material}");
                }
            }
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Requires:\n"));
            foreach (var (part, amount) in Requirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", amount, Loc.GetString(part.ToString())));
            }

            foreach (var (material, amount) in MaterialRequirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", amount, Loc.GetString(material.Name)));
            }

            foreach (var (_, info) in ComponentRequirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", info.Amount, Loc.GetString(info.ExamineName)));
            }
        }
    }

    [Serializable]
    public struct ComponentPartInfo
    {
        public int Amount;
        public string ExamineName;
        public string DefaultPrototype;
    }
}
