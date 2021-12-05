using System.Collections.Generic;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Headset
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
#pragma warning disable 618
    public class HeadsetComponent : Component, IListen, IRadio, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public override string Name => "Headset";

        private RadioSystem _radioSystem = default!;

        [DataField("channels")]
        private List<int> _channels = new(){1459};

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        private int BroadcastFrequency { get; set; } = 1459;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; }

        public IReadOnlyList<int> Channels => _channels;

        public bool RadioRequested { get; set; }

        protected override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();
        }

        public bool CanListen(string message, EntityUidsource)
        {
            return RadioRequested;
        }

        public void Receive(string message, int channel, EntityUidsource)
        {
            if (Owner.TryGetContainer(out var container))
            {
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(container.Owner, out ActorComponent? actor))
                    return;

                var playerChannel = actor.PlayerSession.ConnectedClient;

                var msg = _netManager.CreateNetMessage<MsgChatMessage>();

                msg.Channel = ChatChannel.Radio;
                msg.Message = message;
                //Square brackets are added here to avoid issues with escaping
                msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(source).EntityName));
                _netManager.ServerSendMessage(msg, playerChannel);
            }
        }

        public void Listen(string message, EntityUidspeaker)
        {
            Broadcast(message, speaker);
        }

        public void Broadcast(string message, EntityUidspeaker)
        {
            _radioSystem.SpreadMessage(this, speaker, message, BroadcastFrequency);
            RadioRequested = false;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddText(Loc.GetString("examine-radio-frequency", ("frequency", BroadcastFrequency)));
            message.AddText("\n");
            message.AddText(Loc.GetString("examine-headset"));
            message.AddText("\n");
            message.AddText(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
        }
    }
}
