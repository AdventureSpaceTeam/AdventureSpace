using Content.Server.Atmos;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.Temperature.Systems;
using Content.Shared.Inventory;
using Content.Shared.Slippery;
using Content.Shared.Damage;
using Content.Shared.Electrocution;
using Content.Shared.Movement.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Inventory
{
    class InventorySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HumanInventoryControllerComponent, EntRemovedFromContainerMessage>(HandleRemovedFromContainer);
            SubscribeLocalEvent<InventoryComponent, EntRemovedFromContainerMessage>(HandleInvRemovedFromContainer);
            SubscribeLocalEvent<InventoryComponent, HighPressureEvent>(OnHighPressureEvent);
            SubscribeLocalEvent<InventoryComponent, LowPressureEvent>(OnLowPressureEvent);
            SubscribeLocalEvent<InventoryComponent, DamageModifyEvent>(OnDamageModify);
            SubscribeLocalEvent<InventoryComponent, ElectrocutionAttemptEvent>(OnElectrocutionAttempt);
            SubscribeLocalEvent<InventoryComponent, SlipAttemptEvent>(OnSlipAttemptEvent);
            SubscribeLocalEvent<InventoryComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
            SubscribeLocalEvent<InventoryComponent, ModifyChangedTemperatureEvent>(OnModifyTemperature);
        }

        private void OnModifyTemperature(EntityUid uid, InventoryComponent component, ModifyChangedTemperatureEvent args)
        {
            RelayInventoryEvent(component, args);
        }

        private void OnSlipAttemptEvent(EntityUid uid, InventoryComponent component, SlipAttemptEvent args)
        {
            if (component.TryGetSlotItem(EquipmentSlotDefines.Slots.SHOES, out ItemComponent? shoes))
            {
                RaiseLocalEvent(shoes.Owner.Uid, args, false);
            }
        }

        private void OnRefreshMovespeed(EntityUid uid, InventoryComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            RelayInventoryEvent(component, args);
        }

        private static void HandleInvRemovedFromContainer(EntityUid uid, InventoryComponent component, EntRemovedFromContainerMessage args)
        {
            component.ForceUnequip(args.Container, args.Entity);
        }

        private static void HandleRemovedFromContainer(EntityUid uid, HumanInventoryControllerComponent component, EntRemovedFromContainerMessage args)
        {
            component.CheckUniformExists();
        }

        private void OnHighPressureEvent(EntityUid uid, InventoryComponent component, HighPressureEvent args)
        {
            RelayInventoryEvent(component, args);
        }

        private void OnLowPressureEvent(EntityUid uid, InventoryComponent component, LowPressureEvent args)
        {
            RelayInventoryEvent(component, args);
        }

        private void OnElectrocutionAttempt(EntityUid uid, InventoryComponent component, ElectrocutionAttemptEvent args)
        {
            RelayInventoryEvent(component, args);
        }

        private void OnDamageModify(EntityUid uid, InventoryComponent component, DamageModifyEvent args)
        {
            RelayInventoryEvent(component, args);
        }

        private void RelayInventoryEvent<T>(InventoryComponent component, T args) where T : EntityEventArgs
        {
            foreach (var equipped in component.GetAllHeldItems())
            {
                RaiseLocalEvent(equipped.Uid, args, false);
            }
        }
    }
}
