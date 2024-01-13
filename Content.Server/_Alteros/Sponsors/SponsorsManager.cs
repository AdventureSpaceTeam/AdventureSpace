using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Content.Corvax.Interfaces.Server;
using Content.Shared.CCVar;
using Content.Shared.Sponsors;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Sponsors;

public sealed class SponsorsManager : IServerSponsorsManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private string _apiUrl = string.Empty;

    private readonly Dictionary<NetUserId, SponsorInfo> _cachedSponsors = new();

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("sponsors");
        _cfg.OnValueChanged(CCVars.SponsorsApiUrl, s => _apiUrl = s, true);

        _netMgr.RegisterNetMessage<MsgSponsorInfo>();

        _netMgr.Connecting += OnConnecting;
        _netMgr.Connected += OnConnected;
        _netMgr.Disconnect += OnDisconnect;
    }

    public bool TryGetInfo(NetUserId userId, [NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        return _cachedSponsors.TryGetValue(userId, out sponsor);
    }

    public void SetNextAllowRespawn(NetUserId userId, TimeSpan nextRespawnTime)
    {
        if (TryGetInfo(userId, out var sponsor))
            sponsor.NextAllowRespawn = nextRespawnTime;
    }

    public void AddUsedCharactersForRespawn(NetUserId userId, int usedCharacter)
    {
        if (TryGetInfo(userId, out var sponsor))
            sponsor.UsedCharactersForRespawn.Add(usedCharacter);
    }

    public bool TryGetUsedCharactersForRespawn(NetUserId userId, [NotNullWhen(true)] out List<int>? usedCharactersForRespawn)
    {
        usedCharactersForRespawn = null;
        if (!TryGetInfo(userId, out var sponsor))
            return false;
        usedCharactersForRespawn = sponsor.UsedCharactersForRespawn;
        return true;
    }

    public bool TryGetNextAllowRespawn(NetUserId userId, [NotNullWhen(true)] out TimeSpan? nextAllowRespawn)
    {
        nextAllowRespawn = null;
        if (!TryGetInfo(userId, out var sponsor))
            return false;
        nextAllowRespawn = sponsor.NextAllowRespawn;
        return true;
    }

    public ICommonSession PickSession(List<ICommonSession> sessions, string roleId)
    {
        var prioritySessions = PickPrioritySessions(sessions, roleId);
        var session = _random.PickAndTake(sessions);
        if (prioritySessions.Count != 0)
        {
            session = _random.PickAndTake(prioritySessions);
        }

        sessions.Remove(session);
        return session;
    }

    private List<ICommonSession> PickPrioritySessions(List<ICommonSession> sessions, string roleId)
    {
        List<ICommonSession> prioritySessions = new();
        foreach (var session in sessions)
        {
            if (!TryGetPriorityAntags(session.UserId, out var priorityRoles))
                continue;
            if (priorityRoles.Contains(roleId))
                prioritySessions.Add(session);
        }

        return prioritySessions;
    }

    private async Task OnConnecting(NetConnectingArgs e)
    {
        var info = await LoadSponsorInfo(e.UserId);
        if (info?.Tier == null)
        {
            _cachedSponsors.Remove(e.UserId); // Remove from cache if sponsor expired
            return;
        }

        DebugTools.Assert(!_cachedSponsors.ContainsKey(e.UserId), "Cached data was found on client connect");

        _cachedSponsors[e.UserId] = info;
    }

    private void OnConnected(object? sender, NetChannelArgs e)
    {
        var info = _cachedSponsors.TryGetValue(e.Channel.UserId, out var sponsor) ? sponsor : null;
        var msg = new MsgSponsorInfo() { Info = info };
        _netMgr.ServerSendMessage(msg, e.Channel);

    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cachedSponsors.Remove(e.Channel.UserId);
    }

    private async Task<SponsorInfo?> LoadSponsorInfo(NetUserId userId)
    {
        if (string.IsNullOrEmpty(_apiUrl))
            return null;

        var url = $"{_apiUrl}/sponsors/?user_id={userId.ToString()}&";
        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _sawmill.Error(
                "Failed to get player sponsor OOC color from API: [{StatusCode}] {Response}",
                response.StatusCode,
                errorText);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SponsorInfo>();
    }

    public bool TryGetGhostTheme(NetUserId userId, [NotNullWhen(true)] out string? ghostTheme)
    {
        if (!_cachedSponsors.ContainsKey(userId) || string.IsNullOrEmpty(_cachedSponsors[userId].GhostTheme))
        {
            ghostTheme = null;
            return false;
        }

        ghostTheme = _cachedSponsors[userId].GhostTheme!;
        return true;
    }

    public bool TryGetPrototypes(NetUserId userId, [NotNullWhen(true)]  out List<string>? prototypes)
    {
        prototypes = null;
        if (!TryGetInfo(userId, out var sponsor))
            return false;

        prototypes = new List<string>();
        prototypes.AddRange(sponsor.AllowedMarkings);
        prototypes.AddRange(sponsor.AllowedSpecies);
        prototypes.AddRange(sponsor.OpenAntags);
        prototypes.AddRange(sponsor.OpenRoles);
        prototypes.AddRange(sponsor.OpenGhostRoles);

        return true;
    }

    public bool TryGetPriorityAntags(NetUserId userId, [NotNullWhen(true)]  out List<string>? priorityAntags)
    {
        priorityAntags = null;
        if (!TryGetInfo(userId, out var sponsor))
            return false;

        priorityAntags = new List<string>();
        priorityAntags.AddRange(sponsor.PriorityAntags);
        return true;
    }

    public bool TryGetOocTitle(NetUserId userId, [NotNullWhen(true)] out string? title)
    {
        if (!_cachedSponsors.ContainsKey(userId) || _cachedSponsors[userId].Tier == null)
        {
            title = null;
            return false;
        }

        title = _cachedSponsors[userId].Title;

        return title != null;
    }

    public bool TryGetOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color)
    {
        if (!_cachedSponsors.ContainsKey(userId) || _cachedSponsors[userId].OOCColor == null)
        {
            color = null;
            return false;
        }

        color = Color.TryFromHex(_cachedSponsors[userId].OOCColor);

        return color != null;
    }

    public int GetExtraCharSlots(NetUserId userId)
    {
        return !_cachedSponsors.ContainsKey(userId) ? 0 : _cachedSponsors[userId].ExtraSlots;
    }

    public bool HavePriorityJoin(NetUserId userId)
    {
        return _cachedSponsors.ContainsKey(userId) && _cachedSponsors[userId].HavePriorityJoin;
    }
    public bool IsSponsor(NetUserId userId)
    {
        return _cachedSponsors.ContainsKey(userId);
    }

    public bool AllowedRespawn(NetUserId userId)
    {
        return _cachedSponsors.ContainsKey(userId) && _cachedSponsors[userId].AllowedRespawn;
    }
}
