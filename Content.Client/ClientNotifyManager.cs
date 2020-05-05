using System;
using System.Collections.Generic;
using Content.Client.Interfaces;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client
{
    public class ClientNotifyManager : SharedNotifyManager, IClientNotifyManager
    {
#pragma warning disable 649
        [Dependency] private IPlayerManager _playerManager;
        [Dependency] private IUserInterfaceManager _userInterfaceManager;
        [Dependency] private IInputManager _inputManager;
        [Dependency] private IEyeManager _eyeManager;
        [Dependency] private IClientNetManager _netManager;
#pragma warning restore 649

        private readonly List<PopupLabel> _aliveLabels = new List<PopupLabel>();
        private bool _initialized;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgDoNotify>(nameof(MsgDoNotify), DoNotifyMessage);

            _initialized = true;
        }

        private void DoNotifyMessage(MsgDoNotify message)
        {
            PopupMessage(_eyeManager.WorldToScreen(message.Coordinates), message.Message);
        }

        public override void PopupMessage(GridCoordinates coordinates, IEntity viewer, string message)
        {
            if (viewer != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            PopupMessage(_eyeManager.WorldToScreen(coordinates), message);
        }

        public void PopupMessage(ScreenCoordinates coordinates, string message)
        {
            var label = new PopupLabel
            {
                Text = message,
                StyleClasses = { StyleNano.StyleClassPopupMessage },
            };
            var minimumSize = label.CombinedMinimumSize;
            LayoutContainer.SetPosition(label, label.InitialPos = coordinates.Position - minimumSize / 2);
            _userInterfaceManager.PopupRoot.AddChild(label);
            _aliveLabels.Add(label);
        }

        public void PopupMessage(string message)
        {
            PopupMessage(new ScreenCoordinates(_inputManager.MouseScreenPosition), message);
        }

        public void FrameUpdate(FrameEventArgs eventArgs)
        {
            _aliveLabels.ForEach(l =>
            {
                if (l.TimeLeft > 3f)
                {
                    l.Dispose();
                }
            });

            _aliveLabels.RemoveAll(l => l.Disposed);
        }

        private class PopupLabel : Label
        {
            public float TimeLeft { get; private set; }
            public Vector2 InitialPos { get; set; }

            public PopupLabel()
            {
                ShadowOffsetXOverride = 1;
                ShadowOffsetYOverride = 1;
                FontColorShadowOverride = Color.Black;
            }

            protected override void Update(FrameEventArgs eventArgs)
            {
                TimeLeft += eventArgs.DeltaSeconds;
                LayoutContainer.SetPosition(this, InitialPos - (0, 20 * (TimeLeft * TimeLeft + TimeLeft)));
                if (TimeLeft > 0.5f)
                {
                    Modulate = Color.White.WithAlpha(1f - 0.2f * (float)Math.Pow(TimeLeft - 0.5f, 3f));
                }
            }
        }
    }

    public class PopupMessageCommand : IConsoleCommand
    {
        public string Command => "popupmsg";
        public string Description => "";
        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var arg = args[0];
            var mgr = IoCManager.Resolve<IClientNotifyManager>();
            mgr.PopupMessage(arg);
            return false;
        }
    }
}
