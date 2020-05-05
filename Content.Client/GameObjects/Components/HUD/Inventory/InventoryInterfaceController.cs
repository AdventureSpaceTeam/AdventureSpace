﻿using System;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects
{
    public abstract class InventoryInterfaceController : IDisposable
    {
#pragma warning disable 649
        [Dependency] protected readonly IGameHud _gameHud;
#pragma warning restore 649

        protected InventoryInterfaceController(ClientInventoryComponent owner)
        {
            Owner = owner;
        }

        public virtual void Initialize()
        {

        }

        public abstract SS14Window Window { get; }
        protected ClientInventoryComponent Owner { get; }

        public virtual void PlayerAttached()
        {
            _gameHud.InventoryButtonVisible = true;
            _gameHud.InventoryButtonToggled = b =>
            {
                if (b)
                {
                    Window.Open();
                }
                else
                {
                    Window.Close();
                }
            };
        }

        public virtual void PlayerDetached()
        {
            _gameHud.InventoryButtonVisible = false;
            Window.Close();
        }

        public virtual void Dispose()
        {
        }

        public virtual void AddToSlot(EquipmentSlotDefines.Slots slot, IEntity entity)
        {
        }

        public virtual void RemoveFromSlot(EquipmentSlotDefines.Slots slot)
        {
        }

        protected virtual void HandleInventoryKeybind(GUIBoundKeyEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                UseItemOnInventory(slot);
            }
        }

        protected void AddToInventory(GUIBoundKeyEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            Owner.SendEquipMessage(slot);
        }

        protected void UseItemOnInventory(EquipmentSlotDefines.Slots slot)
        {
            Owner.SendUseMessage(slot);
        }

        protected void OpenStorage(GUIBoundKeyEventArgs args, EquipmentSlotDefines.Slots slot)
        {
            if (args.Function != EngineKeyFunctions.UIClick && args.Function != ContentKeyFunctions.ActivateItemInWorld)
            {
                return;
            }

            Owner.SendOpenStorageUIMessage(slot);
        }
    }
}
