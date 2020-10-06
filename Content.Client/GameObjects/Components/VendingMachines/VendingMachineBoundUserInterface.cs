﻿using Content.Client.VendingMachines;
using Content.Shared.GameObjects.Components.VendingMachines;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.VendingMachines
{
    class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu _menu;

        public SharedVendingMachineComponent VendingMachine { get; private set; }

        public VendingMachineBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new SharedVendingMachineComponent.InventorySyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            if(!Owner.Owner.TryGetComponent(out SharedVendingMachineComponent vendingMachine))
            {
                return;
            }

            VendingMachine = vendingMachine;

            _menu = new VendingMachineMenu() { Owner = this, Title = Owner.Owner.Name };
            _menu.Populate(VendingMachine.Inventory);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void Eject(string ID)
        {
            SendMessage(new SharedVendingMachineComponent.VendingMachineEjectMessage(ID));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch(message)
            {
                case SharedVendingMachineComponent.VendingMachineInventoryMessage msg:
                    _menu.Populate(msg.Inventory);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
