using Robust.Shared.Configuration;

namespace Content.Shared.AdventureSpace.CCVars;

public sealed partial class SecretCCVars
{
    public static readonly CVarDef<bool> ContainerSpawnFixEnabled =
        CVarDef.Create("spawn.container_spawn_fix_enabled", true, CVar.SERVER);

    public static readonly CVarDef<int> ResearchClientsServersMaxDistance =
        CVarDef.Create("research.clients_servers_max_distance", 1200, CVar.SERVER);

    public static readonly CVarDef<bool> IsTargetDollEnabled =
        CVarDef.Create("surgery.targetdoll_enabled", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> IsCharHighlightEnabled =
        CVarDef.Create("chat.highlight_enabled", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> IsDropTimeEnabled =
        CVarDef.Create("game.drop_time_enabled", false, CVar.SERVER);
}
