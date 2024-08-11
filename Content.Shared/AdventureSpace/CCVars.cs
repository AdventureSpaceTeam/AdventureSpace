using Robust.Shared.Configuration;

namespace Content.Shared.AdventureSpace;

[CVarDefs]
public sealed partial class AccVars
{
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY);
}
