using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.DiscordAuth;
using Content.Shared.DiscordMember;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.DiscordAuth;

public sealed class DiscordAuthManager : Content.Corvax.Interfaces.Server.IServerDiscordAuthManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IPlayerManager _playerMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;
    private readonly HttpClient _httpClient = new();
    private bool _isEnabled;
    private bool _checkMember;
    private string _apiUrl = string.Empty;
    private string _apiKey = string.Empty;
    private string _discordLink = string.Empty;

    /// <summary>
    ///     Raised when player passed verification or if feature disabled
    /// </summary>
    public event EventHandler<ICommonSession>? PlayerVerified;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("discord_auth");

        _cfg.OnValueChanged(CCVars.DiscordAuthEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CCVars.DiscordAuthApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCVars.DiscordAuthApiKey, v => _apiKey = v, true);
        _cfg.OnValueChanged(CCVars.InfoLinksDiscord, v => _discordLink = v, true);
        _cfg.OnValueChanged(CCVars.DiscordAuthCheckMember, v => _checkMember = v, true);

        _netMgr.RegisterNetMessage<MsgDiscordAuthRequired>();
        _netMgr.RegisterNetMessage<MsgDiscordMemberRequired>();
        _netMgr.RegisterNetMessage<MsgDiscordAuthCheck>(OnAuthCheck);
        _netMgr.RegisterNetMessage<MsgDiscordMemberCheck>(OnMemberCheck);

        _playerMgr.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private async void OnAuthCheck(MsgDiscordAuthCheck message)
    {
        var isVerified = await IsVerified(message.MsgChannel.UserId);
        var session = _playerMgr.GetSessionByUserId(message.MsgChannel.UserId);
        if (isVerified)
        {
            if (_checkMember)
            {
                var isDiscordMember = await IsDiscordMember(message.MsgChannel.UserId);

                if (isDiscordMember)
                {
                    PlayerVerified?.Invoke(this, session);
                    return;
                }

                var joinUrl = await GenerateDiscordLink(message.MsgChannel.UserId);
                var user = await GetDiscordUser(message.MsgChannel.UserId);
                var joinMsg = new MsgDiscordMemberRequired() { AuthUrl = joinUrl.Url, QrCode = joinUrl.Qrcode, DiscordUsername = user.Username };
                session.ConnectedClient.SendMessage(joinMsg);
                return;
            }

            PlayerVerified?.Invoke(this, session);
        }
    }

    private async void OnMemberCheck(MsgDiscordMemberCheck message)
    {
        var isDiscordMember = await IsDiscordMember(message.MsgChannel.UserId);
        if (isDiscordMember)
        {
            var session = _playerMgr.GetSessionByUserId(message.MsgChannel.UserId);

            PlayerVerified?.Invoke(this, session);
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected)
            return;

        if (!_isEnabled)
        {
            PlayerVerified?.Invoke(this, e.Session);
            return;
        }

        if (e.NewStatus == SessionStatus.Connected)
        {
            var isVerified = await IsVerified(e.Session.UserId);
            if (isVerified)
            {
                if (_checkMember)
                {
                    var isDiscordMember = await IsDiscordMember(e.Session.UserId);

                    if (isDiscordMember)
                    {
                        PlayerVerified?.Invoke(this, e.Session);
                        return;
                    }

                    var joinUrl = await GenerateDiscordLink(e.Session.UserId);
                    var user = await GetDiscordUser(e.Session.UserId);
                    var joinMsg = new MsgDiscordMemberRequired() { AuthUrl = joinUrl.Url, QrCode = joinUrl.Qrcode, DiscordUsername = user.Username };
                    e.Session.ConnectedClient.SendMessage(joinMsg);
                    return;
                }

                PlayerVerified?.Invoke(this, e.Session);
                return;
            }

            var authUrl = await GenerateAuthLink(e.Session.UserId);
            var authMsg = new MsgDiscordAuthRequired() { AuthUrl = authUrl.Url, QrCode = authUrl.Qrcode };
            e.Session.ConnectedClient.SendMessage(authMsg);
        }
    }

    public async Task<DiscordGenerateLinkResponse> GenerateAuthLink(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Info($"Player {userId} requested generation Discord verification link");

        var requestUrl = $"{_apiUrl}/generate_auth_link/?user_id={WebUtility.UrlEncode(userId.ToString())}&key={_apiKey}";
        var response = await _httpClient.PostAsync(requestUrl, null, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordGenerateLinkResponse>(cancellationToken: cancel);
        return data!;
    }

    public async Task<DiscordLinkResponse> GenerateDiscordLink(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Info($"Player {userId} requested generation Discord verification link");

        var requestUrl = $"{_apiUrl}/generate_discord_link/?discord_link={_discordLink}&key={_apiKey}";
        var response = await _httpClient.PostAsync(requestUrl, null, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordLinkResponse>(cancellationToken: cancel);
        return data!;
    }

    public async Task<DiscordUserResponse> GetDiscordUser(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Info($"Player {userId} requested discord user info");

        var requestUrl = $"{_apiUrl}/get_discord_user/?user_id={WebUtility.UrlEncode(userId.ToString())}&key={_apiKey}";
        var response = await _httpClient.GetAsync(requestUrl, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancel);
            throw new Exception($"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordUserResponse>(cancellationToken: cancel);
        return data!;
    }

    public async Task<bool> IsVerified(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Debug($"Player {userId} check Discord verification");

        var requestUrl = $"{_apiUrl}/is_verified/?user_id={WebUtility.UrlEncode(userId.ToString())}&key={_apiKey}";
        var response = await _httpClient.GetAsync(requestUrl, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordAuthInfoResponse>(cancellationToken: cancel);
        return data!.IsLinked;
    }

    public async Task<bool> IsDiscordMember(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Debug($"Player {userId} check Discord member");

        var requestUrl = $"{_apiUrl}/is_discord_member/?user_id={WebUtility.UrlEncode(userId.ToString())}&key={_apiKey}";
        var response = await _httpClient.GetAsync(requestUrl, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordMemberInfoResponse>(cancellationToken: cancel);
        return data!.IsMember;
    }

    [UsedImplicitly]
    public sealed record DiscordLinkResponse(string Url, byte[] Qrcode);
    [UsedImplicitly]
    public sealed record DiscordGenerateLinkResponse(string Url, byte[] Qrcode);
    [UsedImplicitly]
    private sealed record DiscordAuthInfoResponse(bool IsLinked);
    [UsedImplicitly]
    private sealed record DiscordMemberInfoResponse(bool IsMember);
    [UsedImplicitly]
    public sealed record DiscordUserResponse(string UserId, string Username);

}
