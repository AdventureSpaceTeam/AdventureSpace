using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Server.Adventure.Config;

public sealed class AdventureConfigManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    private const string path = "/ConfigPresets/Adventure/config.toml";

    public void Initialize()
    {
        var sawmill = _log.GetSawmill("adventure_config");
        if (!_res.TryContentFileRead(path, out var file))
        {
            sawmill.Error("Unable to load adventure's config {Preset}!", path);
            return;
        }
        _cfg.LoadDefaultsFromTomlStream(file);
        sawmill.Info("Loaded adventure's config: {Preset}", path);
    }
}
