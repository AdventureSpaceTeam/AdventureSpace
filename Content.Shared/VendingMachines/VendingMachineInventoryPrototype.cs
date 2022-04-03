using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.VendingMachines
{
    [Serializable, NetSerializable, Prototype("vendingMachineInventory")]
    public sealed class VendingMachineInventoryPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [DataField("animationDuration")]
        public double AnimationDuration { get; }

        [DataField("spriteName")]
        public string SpriteName { get; } = string.Empty;

        [DataField("startingInventory")]
        public Dictionary<string, uint> StartingInventory { get; } = new();
    }
}
