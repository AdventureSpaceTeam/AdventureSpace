using System;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Client.GameObjects.Components.Wires
{
    public class WiresBoundUserInterface : BoundUserInterface
    {
        public WiresBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private WiresMenu _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new WiresMenu() {Owner = this};

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (WiresBoundUserInterfaceState) state;
            _menu.Populate(castState.WiresList);
        }

        public void PerformAction(Guid guid, WiresAction action)
        {
            SendMessage(new WiresActionMessage(guid, action));
        }
    }
}
