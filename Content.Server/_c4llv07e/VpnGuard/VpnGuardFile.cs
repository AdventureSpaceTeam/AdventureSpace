using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Content.Corvax.Interfaces.Server;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Content.Server._c4llv07e.VpnGuard;

public sealed class VpnGuardFile : IServerVPNGuardManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;
    public List<IPNetwork> networks = new();

    public void ParseFile()
    {
        int line_number;
        string? content;
        string path;
        path = _cfg.GetCVar(CCVars.VpnGuardFilePath);
        if (path == string.Empty)
            return;
        try {
            using StreamReader stream = new(path);
            line_number = 0;
            while (stream.Peek() >= 0)
            {
                content = stream.ReadLine();
                if (content == null)
                    break;
                line_number += 1;
                if (!IPNetwork.TryParse(content, out var ip_net))
                {
                    _sawmill.Warning($"Can't parse ip network on the line {line_number}");
                }
                networks.Add(ip_net);
            }
            _sawmill.Debug($"Parsed {line_number} lines");
        } catch (FileNotFoundException e) {
            _sawmill.Error($"Can't open vpn list file: {e}");
        }
    }

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("c4_VpnGuardList");
        ParseFile();
    }

    public async Task<bool> IsConnectionVpn(IPAddress ip)
    {
        foreach (var network in networks)
        {
            if (network.Contains(ip))
                return true;
        }
        return false;
    }
}
