using Content.Shared.Chemistry;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.UI
{
    [UsedImplicitly]
    public sealed class TransferAmountBoundUserInterface : BoundUserInterface
    {
        private TransferAmountWindow? _window;

        protected override void Open()
        {
            base.Open();
            _window = new TransferAmountWindow();

            _window.ApplyButton.OnPressed += _ =>
            {
                if (int.TryParse(_window.AmountLineEdit.Text, out var i))
                {
                    SendMessage(new TransferAmountSetValueMessage(FixedPoint2.New(i)));
                    _window.Close();
                }
            };
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        public TransferAmountBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
