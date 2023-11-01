namespace Content.Shared.CCVar
{
    // ReSharper disable once InconsistentNaming
    [CVarDefs]
    public sealed class CCVars : CVars
    {
        /// <summary>
        /// URL of the Discord webhook which will relay all ban messages.
        /// </summary>
        public static readonly CVarDef<string> DiscordBanWebhook =
            CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY);
    }
}
