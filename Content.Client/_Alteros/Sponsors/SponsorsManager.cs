using System.Diagnostics.CodeAnalysis;
using Content.Corvax.Interfaces.Shared;
using Content.Shared.Sponsors;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client.Sponsors;

public sealed class SponsorsManager : ISharedSponsorsManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgSponsorInfo>(OnUpdate);
    }

    private void OnUpdate(MsgSponsorInfo message)
    {
        Reset();

        if (message.Info == null)
        {
            return;
        }

        OocColor = Color.TryFromHex(message.Info.OOCColor);
        OocTitle = message.Info.Title;
        Prototypes.AddRange(message.Info.AllowedMarkings);
        Prototypes.AddRange(message.Info.AllowedSpecies);
        Prototypes.AddRange(message.Info.OpenRoles);
        Prototypes.AddRange(message.Info.OpenGhostRoles);
        Prototypes.AddRange(message.Info.OpenAntags);
        PriorityJoin = message.Info.HavePriorityJoin;
        ExtraCharSlots = message.Info.ExtraSlots;
        GhostTheme = message.Info.GhostTheme;
        AllowedRespawn = message.Info.AllowedRespawn;
    }

    public List<string> GetClientPrototypes() => Prototypes;
    public bool GetClientAllowedRespawn() => AllowedRespawn;
    public int GetServerExtraCharSlots(NetUserId userId) =>
        userId == _playerMan.LocalUser ? ExtraCharSlots : 0;
    public bool HaveServerPriorityJoin(NetUserId userId) =>
        userId == _playerMan.LocalUser ? PriorityJoin : false;
    public bool IsSponsor(NetUserId userId) => throw new NotImplementedException();
    public NetUserId PickRoleSession(HashSet<NetUserId> users, string roleId) => throw new NotImplementedException();

    public bool GetServerAllowedRespawn(NetUserId userId)
    {
        if (userId == _playerMan.LocalUser)
            return AllowedRespawn;
        return false;
    }

    public bool TryGetServerPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes)
    {
        if (userId == _playerMan.LocalUser)
        {
            prototypes = Prototypes;
            return true;
        }
        prototypes = null;
        return false;
    }

    public bool TryGetServerOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color)
    {
        if (userId == _playerMan.LocalUser && OocColor != null)
        {
            color = OocColor;
            return true;
        }
        color = null;
        return false;
    }

    public bool TryGetServerOocTitle(NetUserId userId, [NotNullWhen(true)] out string? title)
    {
        if (userId == _playerMan.LocalUser && OocTitle != null)
        {
            title = OocTitle;
            return true;
        }
        title = null;
        return false;
    }

    public bool TryGetServerGhostTheme(NetUserId userId, [NotNullWhen(true)] out string? ghostTheme)
    {
        if (userId == _playerMan.LocalUser && GhostTheme != null)
        {
            ghostTheme = GhostTheme;
            return true;
        }
        ghostTheme = null;
        return false;
    }

    private void Reset()
    {
        Prototypes.Clear();
        PriorityJoin = false;
        OocColor = null;
        OocTitle = null;
        ExtraCharSlots = 0;
        GhostTheme = null;
    }


    public List<string> Prototypes { get; } = new();
    public bool PriorityJoin { get; private set; }
    public Color? OocColor { get; private set; }
    public string? OocTitle { get; private set; }
    public int ExtraCharSlots { get; private set; }
    public string? GhostTheme { get; private set; }
    public bool AllowedRespawn { get; private set; }
}
