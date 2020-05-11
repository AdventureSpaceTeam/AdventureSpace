﻿using System;
using System.Linq;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Throw;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Input;
using Content.Shared.Interfaces;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HandsSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        private const float ThrowForce = 1.5f; // Throwing force of mobs in Newtons

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerModified);

            var input = EntitySystemManager.GetEntitySystem<InputSystem>();
            input.BindMap.BindFunction(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(HandleSwapHands));
            input.BindMap.BindFunction(ContentKeyFunctions.Drop, new PointerInputCmdHandler(HandleDrop));
            input.BindMap.BindFunction(ContentKeyFunctions.ActivateItemInHand, InputCmdHandler.FromDelegate(HandleActivateItem));
            input.BindMap.BindFunction(ContentKeyFunctions.ThrowItemInHand, new PointerInputCmdHandler(HandleThrowItem));
            input.BindMap.BindFunction(ContentKeyFunctions.SmartEquipBackpack, InputCmdHandler.FromDelegate(HandleSmartEquipBackpack));
            input.BindMap.BindFunction(ContentKeyFunctions.SmartEquipBelt, InputCmdHandler.FromDelegate(HandleSmartEquipBelt));
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            if (EntitySystemManager.TryGetEntitySystem(out InputSystem input))
            {
                input.BindMap.UnbindFunction(ContentKeyFunctions.SwapHands);
                input.BindMap.UnbindFunction(ContentKeyFunctions.Drop);
                input.BindMap.UnbindFunction(ContentKeyFunctions.ActivateItemInHand);
                input.BindMap.UnbindFunction(ContentKeyFunctions.ThrowItemInHand);
            }

            base.Shutdown();
        }

        private static void HandleContainerModified(ContainerModifiedMessage args)
        {
            if (args.Container.Owner.TryGetComponent(out IHandsComponent handsComponent))
            {
                handsComponent.HandleSlotModifiedMaybe(args);
            }
        }

        private static bool TryGetAttachedComponent<T>(IPlayerSession session, out T component)
            where T : Component
        {
            component = default;

            var ent = session.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T comp))
                return false;

            component = comp;
            return true;
        }

        private static void HandleSwapHands(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();

            var oldItem = handsComp.GetActiveHand;

            handsComp.SwapHands();

            var newItem = handsComp.GetActiveHand;

            if(oldItem != null)
                interactionSystem.HandDeselectedInteraction(handsComp.Owner, oldItem.Owner);

            if(newItem != null)
                interactionSystem.HandSelectedInteraction(handsComp.Owner, newItem.Owner);
        }

        private bool HandleDrop(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var ent = ((IPlayerSession) session).AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out HandsComponent handsComp))
                return false;

            if (handsComp.GetActiveHand == null)
                return false;

            var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();

            if(interactionSystem.InRangeUnobstructed(coords.ToMap(_mapManager), ent.Transform.WorldPosition, ignoredEnt: ent))
                if (coords.InRange(_mapManager, ent.Transform.GridPosition, InteractionSystem.InteractionRange))
                {
                    handsComp.Drop(handsComp.ActiveIndex, coords);
                }
                else
                {
                    var entCoords = ent.Transform.GridPosition.Position;
                    var entToDesiredDropCoords = coords.Position - entCoords;
                    var clampedDropCoords = ((entToDesiredDropCoords.Normalized * InteractionSystem.InteractionRange) + entCoords);

                    handsComp.Drop(handsComp.ActiveIndex, new GridCoordinates(clampedDropCoords, coords.GridID));
                }
            else
                handsComp.Drop(handsComp.ActiveIndex, ent.Transform.GridPosition);

            return true;
        }

        private static void HandleActivateItem(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.ActivateItem();
        }

        private bool HandleThrowItem(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var plyEnt = ((IPlayerSession)session).AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return false;

            if (!plyEnt.TryGetComponent(out HandsComponent handsComp))
                return false;

            if (!handsComp.CanDrop(handsComp.ActiveIndex))
                return false;

            var throwEnt = handsComp.GetHand(handsComp.ActiveIndex).Owner;

            if (!handsComp.ThrowItem())
                return false;

            // throw the item, split off from a stack if it's meant to be thrown individually
            if (!throwEnt.TryGetComponent(out StackComponent stackComp) || stackComp.Count < 2 || !stackComp.ThrowIndividually)
            {
                handsComp.Drop(handsComp.ActiveIndex);
            }
            else
            {
                stackComp.Use(1);
                throwEnt = throwEnt.EntityManager.SpawnEntity(throwEnt.Prototype.ID, plyEnt.Transform.GridPosition);

                // can only throw one item at a time, regardless of what the prototype stack size is.
                if (throwEnt.TryGetComponent<StackComponent>(out var newStackComp))
                    newStackComp.Count = 1;
            }

            ThrowHelper.Throw(throwEnt, ThrowForce, coords, plyEnt.Transform.GridPosition, false, plyEnt);

            return true;
        }

        private void HandleSmartEquipBackpack(ICommonSession session)
        {
            HandleSmartEquip(session, EquipmentSlotDefines.Slots.BACKPACK);
        }

        private void HandleSmartEquipBelt(ICommonSession session)
        {
            HandleSmartEquip(session, EquipmentSlotDefines.Slots.BELT);
        }

        private void HandleSmartEquip(ICommonSession session, EquipmentSlotDefines.Slots equipementSlot)
        {
            var plyEnt = ((IPlayerSession) session).AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return;

            if (!plyEnt.TryGetComponent(out HandsComponent handsComp) || !plyEnt.TryGetComponent(out InventoryComponent inventoryComp))
                return;

            if (!inventoryComp.TryGetSlotItem(equipementSlot, out ItemComponent equipmentItem)
                || !equipmentItem.Owner.TryGetComponent<ServerStorageComponent>(out var storageComponent))
            {
                _notifyManager.PopupMessage(plyEnt, plyEnt, Loc.GetString("You have no {0} to take something out of!", EquipmentSlotDefines.SlotNames[equipementSlot].ToLower()));
                return;
            }

            var heldItem = handsComp.GetHand(handsComp.ActiveIndex)?.Owner;

            if (heldItem != null)
            {
                storageComponent.PlayerInsertEntity(plyEnt);
            }
            else
            {
                if (storageComponent.StoredEntities.Count == 0)
                {
                    _notifyManager.PopupMessage(plyEnt, plyEnt, Loc.GetString("There's nothing in your {0} to take out!", EquipmentSlotDefines.SlotNames[equipementSlot].ToLower()));
                }
                else
                {
                    var lastStoredEntity = Enumerable.Last(storageComponent.StoredEntities);
                    if (storageComponent.Remove(lastStoredEntity))
                        handsComp.PutInHandOrDrop(lastStoredEntity.GetComponent<ItemComponent>());
                }
            }
        }
    }
}
