﻿using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Inventory;

[RegisterComponent, NetworkedComponent]
[Access(typeof(InventorySystem))]
public sealed partial class InventoryComponent : Component
{
    [DataField("templateId", customTypeSerializer: typeof(PrototypeIdSerializer<InventoryTemplatePrototype>))]
    public string TemplateId { get; private set; } = "human";

    [DataField("speciesId")] public string? SpeciesId { get; set; }

    public SlotDefinition[] Slots = Array.Empty<SlotDefinition>();
    public ContainerSlot[] Containers = Array.Empty<ContainerSlot>();
}
