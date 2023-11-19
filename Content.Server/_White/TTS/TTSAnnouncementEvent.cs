namespace Content.Server.White.TTS;

public sealed class TTSAnnouncementEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly bool Global;
    public readonly string VoiceId;
    public readonly EntityUid Source;

    public TTSAnnouncementEvent(string message, string voiceId, EntityUid source , bool global)
    {
        Message = message;
        Global = global;
        VoiceId = voiceId;
        Source = source;
    }
}
