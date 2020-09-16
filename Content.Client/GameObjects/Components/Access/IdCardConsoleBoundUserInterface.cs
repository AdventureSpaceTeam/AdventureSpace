using System.Collections.Generic;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.Access.SharedIdCardConsoleComponent;

namespace Content.Client.GameObjects.Components.Access
{
    public class IdCardConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public IdCardConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private IdCardConsoleWindow _window;

        protected override void Open()
        {
            base.Open();

            _window = new IdCardConsoleWindow(this, _prototypeManager);
            _window.Title = Owner.Owner.Name;
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (IdCardConsoleBoundUserInterfaceState) state;
            _window.UpdateState(castState);
        }

        public void ButtonPressed(UiButton button)
        {
            SendMessage(new IdButtonPressedMessage(button));
        }

        public void SubmitData(string newFullName, string newJobTitle, List<string> newAccessList)
        {
            SendMessage(new WriteToTargetIdMessage(
                newFullName,
                newJobTitle,
                newAccessList));
        }
    }
}
