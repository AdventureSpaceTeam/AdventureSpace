using Content.Client.Instruments;
using Robust.Client.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Instruments
{
    public class InstrumentBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private InstrumentMenu _instrumentMenu;

        public InstrumentComponent Instrument { get; set; }

        public InstrumentBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            if (!Owner.Owner.TryGetComponent<InstrumentComponent>(out var instrument)) return;

            Instrument = instrument;
            _instrumentMenu = new InstrumentMenu(this);
            _instrumentMenu.OnClose += Close;

            _instrumentMenu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _instrumentMenu?.Dispose();
        }
    }
}
