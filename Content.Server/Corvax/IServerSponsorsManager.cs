using System.Diagnostics.CodeAnalysis;
using Content.Corvax.Interfaces.Shared;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Corvax.Interfaces.Server;

public interface IServerSponsorsManager : ISharedSponsorsManager
{
    public bool TryGetGhostTheme(NetUserId userId, [NotNullWhen(true)] out string? ghostTheme);
    public bool TryGetPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
    public bool TryGetOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
    public int GetExtraCharSlots(NetUserId userId);
    public bool OpenRoles(NetUserId userId);
    public bool HavePriorityJoin(NetUserId userId);
    public bool HavePriorityRoles(NetUserId userId);
    public ICommonSession PickSession(List<ICommonSession> sessions);
}
