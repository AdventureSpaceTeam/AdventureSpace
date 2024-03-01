using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.DiscordMember;

public sealed class MsgDiscordMemberRequired : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string AuthUrl = string.Empty;
    public string DiscordUsername = string.Empty;
    public byte[] QrCode = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        AuthUrl = buffer.ReadString();
        DiscordUsername = buffer.ReadString();
        buffer.ReadPadBits();
        var length = buffer.ReadInt32();
        if (length == 0)
        {
            return;
        }
        QrCode = buffer.ReadBytes(length);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(AuthUrl);
        buffer.Write(DiscordUsername);
        buffer.WritePadBits();
        buffer.Write((int)QrCode.Length);
        if (QrCode.Length == 0)
        {
            return;
        }
        buffer.Write(QrCode);
    }
}
