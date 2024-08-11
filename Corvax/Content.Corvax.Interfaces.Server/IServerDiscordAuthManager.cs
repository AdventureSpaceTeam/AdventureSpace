using Content.Corvax.Interfaces.Shared;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Corvax.Interfaces.Server;

public interface IServerDiscordAuthManager : ISharedDiscordAuthManager
{
    public event EventHandler<ICommonSession>? PlayerVerified;
    public Task<string> GenerateAuthLink(NetUserId userId, CancellationToken cancel);
    public Task<string> GenerateDiscordLink(NetUserId userId, CancellationToken cancel);
    public Task<bool> IsVerified(NetUserId userId, CancellationToken cancel);
    public Task<bool> IsDiscordMember(NetUserId userId, CancellationToken cancel);
}
