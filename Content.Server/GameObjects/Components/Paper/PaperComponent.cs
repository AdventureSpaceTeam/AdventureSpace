using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class PaperComponent : SharedPaperComponent, IExamine, IAttackBy, IUse
    {

        private BoundUserInterface _userInterface;
        private string _content;
        private PaperAction _mode;

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(PaperUiKey.Key);
            _userInterface.OnReceiveMessage += OnUiReceiveMessage;
            _content = "";
            _mode = PaperAction.Read;
            UpdateUserInterface();
        }
        private void UpdateUserInterface()
        {
            _userInterface.SetState(new PaperBoundUserInterfaceState(_content, _mode));
        }

        public void Examine(FormattedMessage message)
        {
            message.AddMarkup(_content);
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return false;
            _mode = PaperAction.Read;
            UpdateUserInterface();
            _userInterface.Open(actor.playerSession);
            return true;
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            var msg = (PaperInputText) obj.Message;
            if (string.IsNullOrEmpty(msg.Text))
                return;

            _content += msg.Text + '\n';

            if (Owner.TryGetComponent(out SpriteComponent sprite))
            {
                sprite.LayerSetState(0, "paper_words");
            }

            UpdateUserInterface();
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (!eventArgs.AttackWith.HasComponent<WriteComponent>())
                return false;
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return false;

            _mode = PaperAction.Write;
            UpdateUserInterface();
            _userInterface.Open(actor.playerSession);
            return true;
        }
    }
}
