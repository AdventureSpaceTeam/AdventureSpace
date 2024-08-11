using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.Corvax.Interfaces.Shared;

public interface ISharedSponsorsManager
{
    public void Initialize();

    // Client
    public List<string> GetClientPrototypes();
    public bool GetClientAllowedRespawn();

    // Server
    public bool TryGetServerPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
    public bool TryGetServerOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
    public bool TryGetServerOocTitle(NetUserId userId, [NotNullWhen(true)] out string? title);
    public bool TryGetServerGhostTheme(NetUserId userId, [NotNullWhen(true)] out string? ghostTheme);
    public int GetServerExtraCharSlots(NetUserId userId);
    public bool GetServerAllowedRespawn(NetUserId userId);
    public bool HaveServerPriorityJoin(NetUserId userId);
    public bool IsSponsor(NetUserId userId);
    public NetUserId PickRoleSession(HashSet<NetUserId> users, string roleId);
}
