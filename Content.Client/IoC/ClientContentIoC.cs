using Content.Client.Administration.Managers;
using Content.Client.Changelog;
using Content.Client.Chat.Managers;
using Content.Client.Clickable;
using Content.Client.SS220.TTS;
using Content.Client.Options;
using Content.Client.DiscordAuth;
using Content.Client.Eui;
using Content.Client.GhostKick;
using Content.Client.Info;
using Content.Client.Launcher;
using Content.Client.Parallax.Managers;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Preferences;
using Content.Client.Screenshot;
using Content.Client.Fullscreen;
using Content.Client.Stylesheets;
using Content.Client.Viewport;
using Content.Client.Voting;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Module;
using Content.Client.Guidebook;
using Content.Client.JoinQueue;
using Content.Client.Replay;
using Content.Client.Sponsors;
using Content.Shared.Administration.Managers;
using Content.Shared.Players.PlayTimeTracking;


namespace Content.Client.IoC
{
    internal static class ClientContentIoC
    {
        public static void Register()
        {
            var collection = IoCManager.Instance!;

            collection.Register<IParallaxManager, ParallaxManager>();
            collection.Register<IChatManager, ChatManager>();
            collection.Register<IClientPreferencesManager, ClientPreferencesManager>();
            collection.Register<IStylesheetManager, StylesheetManager>();
            collection.Register<IScreenshotHook, ScreenshotHook>();
            collection.Register<FullscreenHook, FullscreenHook>();
            collection.Register<IClickMapManager, ClickMapManager>();
            collection.Register<IClientAdminManager, ClientAdminManager>();
            collection.Register<ISharedAdminManager, ClientAdminManager>();
            collection.Register<EuiManager, EuiManager>();
            collection.Register<IVoteManager, VoteManager>();
            collection.Register<ChangelogManager, ChangelogManager>();
            collection.Register<RulesManager, RulesManager>();
            collection.Register<ViewportManager, ViewportManager>();
            collection.Register<ISharedAdminLogManager, SharedAdminLogManager>();
            collection.Register<GhostKickManager>();
            collection.Register<ExtendedDisconnectInformationManager>();
            collection.Register<JobRequirementsManager>();
            collection.Register<DocumentParsingManager>();
            collection.Register<ContentReplayPlaybackManager, ContentReplayPlaybackManager>();
            collection.Register<ISharedPlaytimeManager, JobRequirementsManager>();

            // Alteros-Sponsor
            collection.Register<Content.Corvax.Interfaces.Client.IClientSponsorsManager,SponsorsManager>();
            collection.Register<Content.Corvax.Interfaces.Client.IClientJoinQueueManager,JoinQueueManager>();
            collection.Register<Content.Corvax.Interfaces.Client.IClientDiscordAuthManager,DiscordAuthManager>();
            // Alteros-Sponsor
        }
    }
}
