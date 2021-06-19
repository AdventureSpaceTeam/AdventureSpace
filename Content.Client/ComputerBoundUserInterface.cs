using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.GameObjects.Components
{
    /// <summary>
    /// ComputerBoundUserInterface shunts all sorts of responsibilities that are in the BoundUserInterface for architectural reasons into the Window.
    /// NOTE: Despite the name, ComputerBoundUserInterface does not and will not care about things like power.
    /// </summary>
    public class ComputerBoundUserInterface<W, S> : ComputerBoundUserInterfaceBase where W : BaseWindow, IComputerWindow<S>, new() where S : BoundUserInterfaceState
    {
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
        private W? _window;

        protected override void Open()
        {
            base.Open();

            _window = (W) _dynamicTypeFactory.CreateInstance(typeof(W));
            _window.SetupComputerWindow(this);
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        // Alas, this constructor has to be copied to the subclass. :(
        public ComputerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null)
            {
                return;
            }

            _window.UpdateState((S) state);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }

    /// <summary>
    /// This class is to avoid a lot of <> being written when we just want to refer to SendMessage.
    /// We could instead qualify a lot of generics even further, but that is a waste of time.
    /// </summary>
    public class ComputerBoundUserInterfaceBase : BoundUserInterface
    {
        public ComputerBoundUserInterfaceBase(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}
        public void SendMessage(BoundUserInterfaceMessage msg)
        {
            base.SendMessage(msg);
        }
    }

    public interface IComputerWindow<S>
    {
        void SetupComputerWindow(ComputerBoundUserInterfaceBase cb) {}
        void UpdateState(S state) {}
    }
}

