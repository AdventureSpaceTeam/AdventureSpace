using System.Threading.Tasks;
using Content.Server.UserInterface;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Paper
{
    [RegisterComponent]
#pragma warning disable 618
    [ComponentReference(typeof(SharedPaperComponent))]
    [ComponentReference(typeof(IActivate))]
    public sealed class PaperComponent : SharedPaperComponent, IExamine, IInteractUsing, IActivate
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private PaperAction _mode;
        [DataField("content")]
        public string Content { get; set; } = "";

        [DataField("contentSize")]
        public int ContentSize { get; set; } = 500;


        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(PaperUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _mode = PaperAction.Read;
            UpdateUserInterface();
        }

        public void SetContent(string content)
        {

            Content = content + '\n';
            UpdateUserInterface();

            if (!_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                return;

            var status = string.IsNullOrWhiteSpace(content)
                ? PaperStatus.Blank
                : PaperStatus.Written;

            appearance.SetData(PaperVisuals.Status, status);
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

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
                return;

            _mode = PaperAction.Read;
            UpdateUserInterface();
            UserInterface?.Toggle(actor.PlayerSession);
            return;
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            var msg = (PaperInputText) obj.Message;
            if (string.IsNullOrEmpty(msg.Text))
                return;


            if (msg.Text.Length + Content.Length <= ContentSize)
                Content += msg.Text + '\n';

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PaperVisuals.Status, PaperStatus.Written);
            }

            _entMan.GetComponent<MetaDataComponent>(Owner).EntityDescription = "";
            UpdateUserInterface();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!EntitySystem.Get<TagSystem>().HasTag(eventArgs.Using, "Write"))
                return false;
            if (!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
                return false;

            _mode = PaperAction.Write;
            UpdateUserInterface();
            UserInterface?.Open(actor.PlayerSession);
            return true;
        }
    }
}
