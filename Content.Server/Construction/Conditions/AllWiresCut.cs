﻿using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     A condition that requires all wires to be cut (or intact)
    ///     Returns true if the entity doesn't have a wires component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class AllWiresCut : IEdgeCondition
    {
        [DataField("value")] public bool Value { get; private set; } = true;

        public async Task<bool> Condition(IEntity entity)
        {
            if (entity.Deleted)
                return false;

            if (!entity.TryGetComponent<WiresComponent>(out var wires))
                return true;

            foreach (var wire in wires.WiresList)
            {
                switch (Value)
                {
                    case true when !wire.IsCut:
                    case false when wire.IsCut:
                        return false;
                }
            }

            return true;
        }
    }
}
