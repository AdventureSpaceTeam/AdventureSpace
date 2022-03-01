using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Inventory;
using Content.Shared.CharacterAppearance;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Content.Client.Clothing;

public sealed class ClothingSystem : EntitySystem
{
    /// <summary>
    /// This is a shitty hotfix written by me (Paul) to save me from renaming all files.
    /// For some context, im currently refactoring inventory. Part of that is slots not being indexed by a massive enum anymore, but by strings.
    /// Problem here: Every rsi-state is using the old enum-names in their state. I already used the new inventoryslots ALOT. tldr: its this or another week of renaming files.
    /// </summary>
    private static readonly Dictionary<string, string> TemporarySlotMap = new()
    {
        {"head", "HELMET"},
        {"eyes", "EYES"},
        {"ears", "EARS"},
        {"mask", "MASK"},
        {"outerClothing", "OUTERCLOTHING"},
        {"jumpsuit", "INNERCLOTHING"},
        {"neck", "NECK"},
        {"back", "BACKPACK"},
        {"belt", "BELT"},
        {"gloves", "HAND"},
        {"shoes", "FEET"},
        {"id", "IDCARD"},
        {"pocket1", "POCKET1"},
        {"pocket2", "POCKET2"},
    };

    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ClothingComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<SharedItemComponent, GetEquipmentVisualsEvent>(OnGetVisuals);

        SubscribeLocalEvent<ClientInventoryComponent, VisualsChangedEvent>(OnVisualsChanged);
        SubscribeLocalEvent<SpriteComponent, DidUnequipEvent>(OnDidUnequip);
    }

    private void OnGetVisuals(EntityUid uid, SharedItemComponent item, GetEquipmentVisualsEvent args)
    {
        if (!TryComp(args.Equipee, out ClientInventoryComponent? inventory))
            return;

        List<PrototypeLayerData>? layers = null;

        // first attempt to get species specific data.
        if (inventory.SpeciesId != null)
            item.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

        // if that returned nothing, attempt to find generic data
        if (layers == null && !item.ClothingVisuals.TryGetValue(args.Slot, out layers))
        {
            // No generic data either. Attempt to generate defaults from the item's RSI & item-prefixes
            if (!TryGetDefaultVisuals(uid, item, args.Slot, inventory.SpeciesId, out layers))
                return;
        }

        // add each layer to the visuals
        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? args.Slot : $"{args.Slot}-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }

    /// <summary>
    ///     If no explicit clothing visuals were specified, this attempts to populate with default values.
    /// </summary>
    /// <remarks>
    ///     Useful for lazily adding clothing sprites without modifying yaml. And for backwards compatibility.
    /// </remarks>
    private bool TryGetDefaultVisuals(EntityUid uid, SharedItemComponent item, string slot, string? speciesId,
        [NotNullWhen(true)] out List<PrototypeLayerData>? layers)
    {
        layers = null;

        RSI? rsi = null;

        if (item.RsiPath != null)
            rsi = _cache.GetResource<RSIResource>(TextureRoot / item.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null || rsi.Path == null)
            return false;

        var correctedSlot = slot;
        TemporarySlotMap.TryGetValue(correctedSlot, out correctedSlot);

        var state = (item.EquippedPrefix == null)
            ? $"equipped-{correctedSlot}"
            : $"{item.EquippedPrefix}-equipped-{correctedSlot}";

        // species specific
        if (speciesId != null && rsi.TryGetState($"{state}-{speciesId}", out _))
        {
            state = $"{state}-{speciesId}";
        }
        else if (!rsi.TryGetState(state, out _))
        {
            return false;
        }

        var layer = PrototypeLayerData.New();
        layer.RsiPath = rsi.Path.ToString();
        layer.State = state;
        layers = new() { layer };

        return true;
    }

    private void OnVisualsChanged(EntityUid uid, ClientInventoryComponent component, VisualsChangedEvent args)
    {
        if (!TryComp(args.Item, out ClothingComponent? clothing) || clothing.InSlot == null)
            return;

        RenderEquipment(uid, args.Item, clothing.InSlot, component, null, clothing);
    }

    private void OnGotUnequipped(EntityUid uid, ClothingComponent component, GotUnequippedEvent args)
    {
        component.InSlot = null;
    }

    private void OnDidUnequip(EntityUid uid, SpriteComponent component, DidUnequipEvent args)
    {
        if (!TryComp(uid, out ClientInventoryComponent? inventory) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!inventory.VisualLayerKeys.TryGetValue(args.Slot, out var revealedLayers))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        foreach (var layer in revealedLayers)
        {
            sprite.RemoveLayer(layer);
        }
        revealedLayers.Clear();
    }

    public void InitClothing(EntityUid uid, ClientInventoryComponent? component = null, SpriteComponent? sprite = null)
    {
        if (!_inventorySystem.TryGetSlots(uid, out var slots, component) || !Resolve(uid, ref sprite, ref component)) return;

        foreach (var slot in slots)
        {
            if (!_inventorySystem.TryGetSlotContainer(uid, slot.Name, out var containerSlot, out _, component) ||
                !containerSlot.ContainedEntity.HasValue) continue;

            RenderEquipment(uid, containerSlot.ContainedEntity.Value, slot.Name, component, sprite);
        }
    }

    private void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        component.InSlot = args.Slot;

        RenderEquipment(args.Equipee, uid, args.Slot, clothingComponent: component);
    }

    private void RenderEquipment(EntityUid equipee, EntityUid equipment, string slot,
        ClientInventoryComponent? inventory = null, SpriteComponent? sprite = null, ClothingComponent? clothingComponent = null)
    {
        if(!Resolve(equipee, ref inventory, ref sprite) || !Resolve(equipment, ref clothingComponent, false))
            return;

        if (slot == "jumpsuit" && sprite.LayerMapTryGet(HumanoidVisualLayers.StencilMask, out _))
        {
            sprite.LayerSetState(HumanoidVisualLayers.StencilMask, clothingComponent.FemaleMask switch
            {
                FemaleClothingMask.NoMask => "female_none",
                FemaleClothingMask.UniformTop => "female_top",
                _ => "female_full",
            });
        }

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        if (inventory.VisualLayerKeys.TryGetValue(slot, out var revealedLayers))
        {
            foreach (var key in revealedLayers)
            {
                sprite.RemoveLayer(key);
            }
            revealedLayers.Clear();
        }
        else
        {
            revealedLayers = new();
            inventory.VisualLayerKeys[slot] = revealedLayers;
        }

        var ev = new GetEquipmentVisualsEvent(equipee, slot);
        RaiseLocalEvent(equipment, ev, false);

        if (ev.Layers.Count == 0)
        {
            RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers));
            return;
        }

        // add the new layers
        foreach (var (key, layerData) in ev.Layers)
        {
            if (!revealedLayers.Add(key))
            {
                Logger.Warning($"Duplicate key for clothing visuals: {key}. Are multiple components attempting to modify the same layer? Equipment: {ToPrettyString(equipment)}");
                continue;
            }

            var index = sprite.LayerMapReserveBlank(key);

            // In case no RSI is given, use the item's base RSI as a default. This cuts down on a lot of unnecessary yaml entries.
            if (layerData.RsiPath == null
                && layerData.TexturePath == null
                && sprite[index].Rsi == null
                && TryComp(equipment, out SpriteComponent? clothingSprite))
            {
                sprite.LayerSetRSI(index, clothingSprite.BaseRSI);
            }

            sprite.LayerSetData(index, layerData);
        }

        RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers));
    }
}
