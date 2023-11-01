using Robust.Shared.Configuration;

namespace Content.Shared.AdventureSpace;

[CVarDefs]
public sealed class CCVars
{
    /// <summary>
    /// URL of the Discord webhook which will show bans in game.
    /// </summary>
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     Should the ban details in admin channel include PII? (IP, HWID, etc)
    public static readonly CVarDef<bool> AdminShowPIIOnBan =
        CVarDef.Create("admin.show_pii_onban", false, CVar.SERVERONLY);
}
