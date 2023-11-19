using Robust.Shared.Serialization;

namespace Content.Shared._White.TTS;

[Serializable, NetSerializable]
public sealed class PlayGlobalTTSEvent : EntityEventArgs
{
    public byte[] Data { get;}
    public PlayGlobalTTSEvent(byte[] data)
    {
        Data = data;
    }
}
