using System.Threading;
using System.Threading.Tasks;
using Content.Corvax.Interfaces.Shared;
using Content.Server.DiscordAuth;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Corvax.Interfaces.Server;

public interface IServerDiscordAuthManager : ISharedDiscordAuthManager
{
    public event EventHandler<ICommonSession>? PlayerVerified;
    public Task<DiscordAuthManager.DiscordGenerateLinkResponse> GenerateAuthLink(NetUserId userId, CancellationToken cancel);
    public Task<DiscordAuthManager.DiscordLinkResponse> GenerateDiscordLink(NetUserId userId, CancellationToken cancel);
    public Task<bool> IsVerified(NetUserId userId, CancellationToken cancel);
    public Task<bool> IsDiscordMember(NetUserId userId, CancellationToken cancel);
}
