using System.IO;
using System.Text.Json.Serialization;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorInfo
{
    [JsonPropertyName("tier")]
    public int? Tier { get; set; }
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("oocColor")]
    public string? OOCColor { get; set; }

    [JsonPropertyName("priorityJoin")]
    public bool HavePriorityJoin { get; set; } = false;

    [JsonPropertyName("extraSlots")]
    public int ExtraSlots { get; set; }

    [JsonPropertyName("ghostTheme")]
    public string? GhostTheme { get; set; }

    [JsonPropertyName("allowedRespawn")]
    public bool AllowedRespawn { get; set; } = false;

    [JsonPropertyName("allowedMarkings")] // TODO: Rename API field in separate PR as breaking change!
    public string[] AllowedMarkings { get; set; } = Array.Empty<string>();

    [JsonPropertyName("allowedSpecies")]
    public string[] AllowedSpecies { get; set; } = Array.Empty<string>();

    [JsonPropertyName("openAntags")]
    public string[] OpenAntags { get; set; } = Array.Empty<string>();

    [JsonPropertyName("openRoles")]
    public string[] OpenRoles { get; set; } = Array.Empty<string>();

    [JsonPropertyName("openGhostRoles")]
    public string[] OpenGhostRoles { get; set; } = Array.Empty<string>();

    [JsonPropertyName("priorityAntags")]
    public string[] PriorityAntags { get; set; } = Array.Empty<string>();

    [JsonPropertyName("nextAllowRespawn")]
    public TimeSpan NextAllowRespawn { get; set; } = TimeSpan.Zero;

    [JsonPropertyName("usedCharactersForReSpawn")]
    public List<int> UsedCharactersForRespawn { get; set; } = new();
}


/// <summary>
/// Server sends sponsoring info to client on connect only if user is sponsor
/// </summary>
public sealed class MsgSponsorInfo : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public SponsorInfo? Info;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var isSponsor = buffer.ReadBoolean();
        buffer.ReadPadBits();
        if (!isSponsor)
            return;

        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream(length);
        buffer.ReadAlignedMemory(stream, length);
        serializer.DeserializeDirect(stream, out Info);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Info != null);
        buffer.WritePadBits();
        if (Info == null)
            return;

        using var stream = new MemoryStream();
        serializer.SerializeDirect(stream, Info);
        buffer.WriteVariableInt32((int) stream.Length);
        stream.TryGetBuffer(out var segment);
        buffer.Write(segment);
    }
}
