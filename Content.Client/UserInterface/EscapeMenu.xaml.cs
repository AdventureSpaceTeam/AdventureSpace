using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface
{
    [GenerateTypedNameReferences]
    internal partial class EscapeMenu : SS14Window
    {
        private readonly IClientConsoleHost _consoleHost;

        private readonly OptionsMenu _optionsMenu;

        public EscapeMenu(IClientConsoleHost consoleHost)
        {
            _consoleHost = consoleHost;

            RobustXamlLoader.Load(this);

            _optionsMenu = new OptionsMenu();

            OptionsButton.OnPressed += OnOptionsButtonClicked;
            QuitButton.OnPressed += OnQuitButtonClicked;
            DisconnectButton.OnPressed += OnDisconnectButtonClicked;
        }

        private void OnQuitButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _consoleHost.ExecuteCommand("quit");
            Dispose();
        }

        private void OnDisconnectButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _consoleHost.ExecuteCommand("disconnect");
            Dispose();
        }

        private void OnOptionsButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _optionsMenu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _optionsMenu.Dispose();
            }
        }
    }
}
