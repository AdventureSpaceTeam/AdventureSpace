using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Content.Corvax.Interfaces.Server;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;

namespace Content.Server._c4llv07e.VpnGuard;

public sealed class VpnGuardApi : IServerVPNGuardManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;
    private Uri _apiUri = default!;
    private string _userId = string.Empty;
    private string _apiKey = string.Empty;

    private readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    private void UpdateUri(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var newUri))
            _apiUri = newUri;
    }

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("c4_VpnGuardApi");
        _cfg.OnValueChanged(CCVars.VpnGuardApiUrl, UpdateUri, true);
        _cfg.OnValueChanged(CCVars.VpnGuardApiUserId, s => _userId = s, true);
        _cfg.OnValueChanged(CCVars.VpnGuardApiKey, s => _apiKey = s, true);
    }

    public async Task<bool> IsConnectionVpn(IPAddress ip)
    {
        if (_apiUri == null || _userId == string.Empty || _apiKey == string.Empty)
            return false;
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = _apiUri,
            Headers = {
                { "User-ID", _userId },
                { "API-Key", _apiKey },
            },
            Content = new StringContent(ip.ToString()),
        };
        try {
            var resp = await _httpClient.SendAsync(request);
            var json = System.Text.Json.JsonDocument.Parse(resp.Content.ReadAsStream());
            var isVpn = json.RootElement.GetProperty("is-vpn").GetBoolean();
            var isProxy = json.RootElement.GetProperty("is-proxy").GetBoolean();
            _sawmill.Debug($"Ip {ip}: vpn: {isVpn}, proxy: {isProxy}");
            return isVpn || isProxy;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _sawmill.Error("Vpn check timeout");
            return false;
        }
    }
}
