using System.Diagnostics.CodeAnalysis;
using Content.Corvax.Interfaces.Shared;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Corvax.Interfaces.Server;

public interface IServerSponsorsManager : ISharedSponsorsManager
{
    public bool TryGetGhostTheme(NetUserId userId, [NotNullWhen(true)] out string? ghostTheme);
    public bool TryGetPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
    public bool TryGetOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
    public bool TryGetOocTitle(NetUserId userId, [NotNullWhen(true)] out string? title);
    public int GetExtraCharSlots(NetUserId userId);
    public bool HavePriorityJoin(NetUserId userId);
    public bool IsSponsor(NetUserId userId);
    public bool AllowedRespawn(NetUserId userId);
    public bool TryGetNextAllowRespawn(NetUserId userId, [NotNullWhen(true)] out TimeSpan? nextAllowRespawn);
    public bool TryGetUsedCharactersForRespawn(NetUserId userId, [NotNullWhen(true)] out List<int>? usedCharactersForRespawn);

    public ICommonSession PickSession(List<ICommonSession> sessions, string roleId);

    public void SetNextAllowRespawn(NetUserId userId, TimeSpan nextRespawnTime);

    public void AddUsedCharactersForRespawn(NetUserId userId, int usedCharacter);
}
