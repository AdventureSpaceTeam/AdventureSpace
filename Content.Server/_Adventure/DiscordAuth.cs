using JetBrains.Annotations;

namespace Content.Server.DiscordAuth;

public sealed partial class DiscordAuthManager
{
    [UsedImplicitly]
    public sealed record DiscordLinkResponse(string Url, byte[] Qrcode);
    [UsedImplicitly]
    public sealed record DiscordGenerateLinkResponse(string Url, byte[] Qrcode);
}
