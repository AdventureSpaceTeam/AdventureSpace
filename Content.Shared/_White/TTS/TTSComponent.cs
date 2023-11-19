using Content.Shared.White.TTS;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.TTS;

[RegisterComponent]
// ReSharper disable once InconsistentNaming
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// Prototype of used voice for TTS.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype> VoicePrototypeId { get; set; } = string.Empty;
}
