#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.Administration;
using Content.Shared;
using Robust.Client.Credits;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Network;
using Robust.Shared.GameObjects;
using YamlDotNet.RepresentationModel;

namespace Content.Client.Administration.UI
{
    /// <summary>
    /// This window connects to a BwoinkSystem channel. BwoinkSystem manages the rest.
    /// </summary>
    [GenerateTypedNameReferences]
    public partial class BwoinkWindow : SS14Window
    {
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;

        private readonly NetUserId _channelId;

        public BwoinkWindow(NetUserId channelId, string title)
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            Title = title;
            _channelId = channelId;

            SenderLineEdit.OnTextEntered += Input_OnTextEntered;

            MinSize = (650, 450);
        }

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                var bwoink = _systemManager.GetEntitySystem<BwoinkSystem>();
                bwoink.Send(_channelId, args.Text);
            }

            SenderLineEdit.Clear();
        }

        public void ReceiveLine(string text)
        {
            var formatted = new FormattedMessage(1);
            formatted.AddText(text);
            TextOutput.AddMessage(formatted);
        }
    }
}
