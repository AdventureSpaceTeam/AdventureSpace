#nullable enable
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Tag;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Paper
{
    [RegisterComponent]
    public class PaperComponent : SharedPaperComponent, IExamine, IInteractUsing, IUse
    {
        private PaperAction _mode;
        [DataField("content")]
        public string Content { get; private set; } = "";

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(PaperUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _mode = PaperAction.Read;
            UpdateUserInterface();
        }
        private void UpdateUserInterface()
        {
            UserInterface?.SetState(new PaperBoundUserInterfaceState(Content, _mode));
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange)
                return;
            if (Content == "")
                return;

            message.AddMarkup(
                Loc.GetString(
                    "paper-component-examine-detail-has-words"
                )
            );
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
                return false;

            _mode = PaperAction.Read;
            UpdateUserInterface();
            UserInterface?.Toggle(actor.PlayerSession);
            return true;
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            var msg = (PaperInputText) obj.Message;
            if (string.IsNullOrEmpty(msg.Text))
                return;

            Content += msg.Text + '\n';

            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.LayerSetState(0, "paper_words");
            }

            Owner.Description = "";
            UpdateUserInterface();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.HasTag("Write"))
                return false;
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
                return false;

            _mode = PaperAction.Write;
            UpdateUserInterface();
            UserInterface?.Open(actor.PlayerSession);
            return true;
        }
    }
}
