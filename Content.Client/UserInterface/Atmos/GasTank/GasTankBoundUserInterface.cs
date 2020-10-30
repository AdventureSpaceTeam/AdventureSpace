﻿using Content.Shared.GameObjects.Components.Atmos.GasTank;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Client.UserInterface.Atmos.GasTank
{
    [UsedImplicitly]
    public class GasTankBoundUserInterface
        : BoundUserInterface
    {
        public GasTankBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) :
            base(owner, uiKey)
        {
        }

        private GasTankWindow _window;

        public void SetOutputPressure(in float value)
        {
            SendMessage(new GasTankSetPressureMessage {Pressure = value});
        }

        public void ToggleInternals()
        {
            SendMessage(new GasTankToggleInternalsMessage());
        }

        protected override void Open()
        {
            base.Open();
            _window = new GasTankWindow(this);
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window.UpdateState((GasTankBoundUserInterfaceState) state);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _window.Close();
        }
    }
}
