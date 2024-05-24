using Content.Corvax.Interfaces.Server;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Content.Server._c4llv07e.VpnGuard;

public sealed class VpnGuard : IServerVPNGuardManager
{
    private readonly Uri _checkerUri = new("https://neutrinoapi.net/ip-probe");
    // private readonly Uri _checkerUri = new("http://localhost/ip-probe");
    private const string _userId = "id";
    private const string _apiKey = "key";

    private readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    public void Initialize()
    {
    }

    public async Task<bool> IsConnectionVpn(IPAddress ip)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = _checkerUri,
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
            return isVpn || isProxy;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            var sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("VpnGuard");
            sawmill.Error("Vpn check timeout");
            return false;
        }
    }
}
