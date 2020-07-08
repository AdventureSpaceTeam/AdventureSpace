using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;

namespace Content.Shared.Chat
{
    /// <summary>
    ///     Sent from server to client to notify the client about a new chat message.
    /// </summary>
    public sealed class MsgChatMessage : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgChatMessage);
        public MsgChatMessage(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        /// <summary>
        ///     The channel the message is on. This can also change whether certain params are used.
        /// </summary>
        public ChatChannel Channel { get; set; }

        /// <summary>
        ///     The actual message contents.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     What to "wrap" the message contents with. Example is stuff like 'Joe says: "{0}"'
        /// </summary>
        public string MessageWrap { get; set; }

        /// <summary>
        ///     The sending entity.
        ///     Only applies to <see cref="ChatChannel.Local"/>, <see cref="ChatChannel.Dead"/> and <see cref="ChatChannel.Emotes"/>.
        /// </summary>
        public EntityUid SenderEntity { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Channel = (ChatChannel) buffer.ReadInt16();
            Message = buffer.ReadString();
            MessageWrap = buffer.ReadString();

            switch (Channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Dead:
                case ChatChannel.AdminChat:
                case ChatChannel.Emotes:
                    SenderEntity = buffer.ReadEntityUid();
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((short)Channel);
            buffer.Write(Message);
            buffer.Write(MessageWrap);

            switch (Channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Dead:
                case ChatChannel.AdminChat:
                case ChatChannel.Emotes:
                    buffer.Write(SenderEntity);
                    break;
            }
        }
    }
}
