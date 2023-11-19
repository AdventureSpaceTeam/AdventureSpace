using Content.Shared._White.TTS;
using Content.Shared.White.TTS;
using Robust.Shared.Network;

namespace Content.Client.White.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgRequestTTS>();
    }

    // ReSharper disable once InconsistentNaming
    public void RequestTTS(EntityUid uid, string text, string voiceId)
    {
        var msg = new MsgRequestTTS() { Text = text, Uid = uid, VoiceId = voiceId };
        _netMgr.ClientSendMessage(msg);
    }

    public void RequestGlobalTTS(string text, string voiceId, IEntityManager entityManager)
    {
        entityManager.EntityNetManager?.SendSystemNetworkMessage(new RequestGlobalTTSEvent(text, voiceId));
    }
}
