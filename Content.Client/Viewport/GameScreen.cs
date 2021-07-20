using Content.Client.Administration.Managers;
using Content.Client.Chat;
using Content.Client.Chat.Managers;
using Content.Client.Chat.UI;
using Content.Client.Construction.UI;
using Content.Client.HUD;
using Content.Client.HUD.UI;
using Content.Client.Voting;
using Content.Shared.Chat;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Viewport
{
    public class GameScreen : GameScreenBase, IMainViewportState
    {
        public static readonly Vector2i ViewportSize = (EyeManager.PixelsPerMeter * 21, EyeManager.PixelsPerMeter * 15);

        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        [ViewVariables] private ChatBox? _gameChat;
        private ConstructionMenuPresenter? _constructionMenu;

        public MainViewport Viewport { get; private set; } = default!;

        public override void Startup()
        {
            base.Startup();

            _gameChat = new HudChatBox {PreferredChannel = ChatSelectChannel.Local};

            UserInterfaceManager.StateRoot.AddChild(_gameChat);
            LayoutContainer.SetAnchorAndMarginPreset(_gameChat, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetAnchorAndMarginPreset(_gameChat, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetMarginLeft(_gameChat, -475);
            LayoutContainer.SetMarginBottom(_gameChat, HudChatBox.InitialChatBottom);

            _chatManager.ChatBoxOnResized(new ChatResizedEventArgs(HudChatBox.InitialChatBottom));

            Viewport = new MainViewport
            {
                Viewport =
                {
                    ViewportSize = ViewportSize
                }
            };

            _userInterfaceManager.StateRoot.AddChild(Viewport);
            LayoutContainer.SetAnchorPreset(Viewport, LayoutContainer.LayoutPreset.Wide);
            Viewport.SetPositionFirst();

            _userInterfaceManager.StateRoot.AddChild(_gameHud.RootControl);
            _chatManager.SetChatBox(_gameChat);
            _voteManager.SetPopupContainer(_gameHud.VoteContainer);

            ChatInput.SetupChatInputHandlers(_inputManager, _gameChat);

            SetupPresenters();

            _eyeManager.MainViewport = Viewport.Viewport;
        }

        public override void Shutdown()
        {
            DisposePresenters();

            base.Shutdown();

            _gameChat?.Dispose();
            Viewport.Dispose();
            _gameHud.RootControl.Orphan();
            // Clear viewport to some fallback, whatever.
            _eyeManager.MainViewport = _userInterfaceManager.MainViewport;
        }

        /// <summary>
        /// All UI Presenters should be constructed in here.
        /// </summary>
        private void SetupPresenters()
        {
            _constructionMenu = new ConstructionMenuPresenter(_gameHud);
        }

        /// <summary>
        /// All UI Presenters should be disposed in here.
        /// </summary>
        private void DisposePresenters()
        {
            _constructionMenu?.Dispose();
        }

        internal static void FocusChat(ChatBox chat)
        {
            if (chat.UserInterfaceManager.KeyboardFocused != null)
                return;

            chat.Focus();
        }

        internal static void FocusChannel(ChatBox chat, ChatSelectChannel channel)
        {
            if (chat.UserInterfaceManager.KeyboardFocused != null)
                return;

            chat.Focus(channel);
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            base.FrameUpdate(e);

            Viewport.Viewport.Eye = _eyeManager.CurrentEye;
        }

        protected override void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
        {
            if (args.Viewport == null)
                base.OnKeyBindStateChanged(new ViewportBoundKeyEventArgs(args.KeyEventArgs, Viewport.Viewport));
            else
                base.OnKeyBindStateChanged(args);
        }
    }
}
