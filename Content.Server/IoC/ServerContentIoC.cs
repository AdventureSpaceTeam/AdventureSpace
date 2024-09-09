using Content.Server._DTS;
using Content.Server._c4llv07e;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Notes;
using Content.Server.Afk;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Server.DiscordAuth;
using Content.Server.EUI;
using Content.Server.GhostKick;
using Content.Server.Info;
using Content.Server.Mapping;
using Content.Server.JoinQueue;
using Content.Server.Maps;
using Content.Server.MoMMI;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Objectives;
using Content.Server.Players;
using Content.Server.Players.JobWhitelist;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Players.RateLimiting;
using Content.Server.Preferences.Managers;
using Content.Server.ServerInfo;
using Content.Server.ServerUpdates;
using Content.Server.Sponsors;
using Content.Server.Corvax.TTS;
using Content.Server.Voting.Managers;
using Content.Server.Worldgen.Tools;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.Kitchen;
using Content.Shared.Players.PlayTimeTracking;
using Content.Corvax.Interfaces.Shared;
using Content.Corvax.Interfaces.Server;
using Content.Server._c4llv07e.VpnGuard; // c4llv07e vpn guard
using Content.Server.Adventure.Config; // c4llv07e adventure config

namespace Content.Server.IoC
{
    internal static class ServerContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IChatSanitizationManager, ChatSanitizationManager>();
            IoCManager.Register<IMoMMILink, MoMMILink>();
            IoCManager.Register<IServerPreferencesManager, ServerPreferencesManager>();
            IoCManager.Register<IServerDbManager, ServerDbManager>();
            IoCManager.Register<RecipeManager, RecipeManager>();
            IoCManager.Register<INodeGroupFactory, NodeGroupFactory>();
            IoCManager.Register<IConnectionManager, ConnectionManager>();
            IoCManager.Register<ServerUpdateManager>();
            IoCManager.Register<IAdminManager, AdminManager>();
            IoCManager.Register<ISharedAdminManager, AdminManager>();
            IoCManager.Register<EuiManager, EuiManager>();
            IoCManager.Register<IVoteManager, VoteManager>();
            IoCManager.Register<IPlayerLocator, PlayerLocator>();
            IoCManager.Register<IAfkManager, AfkManager>();
            IoCManager.Register<MapSystem, MapSystem>(); // c4llv07e edit
            IoCManager.Register<IGameMapManager, DTSMapManager>(); // DTS EDIT
            IoCManager.Register<RulesManager, RulesManager>();
            IoCManager.Register<IBanManager, BanManager>();
            IoCManager.Register<ContentNetworkResourceManager>();
            IoCManager.Register<IAdminNotesManager, AdminNotesManager>();
            IoCManager.Register<GhostKickManager>();
            IoCManager.Register<ISharedAdminLogManager, AdminLogManager>();
            IoCManager.Register<IAdminLogManager, AdminLogManager>();
            IoCManager.Register<PlayTimeTrackingManager>();
            IoCManager.Register<UserDbDataManager>();
            IoCManager.Register<TTSManager>(); // Corvax-TTS
            IoCManager.Register<ServerInfoManager>();
            IoCManager.Register<PoissonDiskSampler>();
            IoCManager.Register<DiscordWebhook>();
            IoCManager.Register<ServerDbEntryManager>();
            IoCManager.Register<ISharedPlaytimeManager, PlayTimeTrackingManager>();
            IoCManager.Register<ServerApi>();
            IoCManager.Register<JobWhitelistManager>();
            IoCManager.Register<PlayerRateLimitManager>();
            IoCManager.Register<MappingManager>();

            // Alteros-Sponsor
            IoCManager.Register<Content.Corvax.Interfaces.Shared.ISharedSponsorsManager, SponsorsManager>();
            IoCManager.Register<Content.Corvax.Interfaces.Server.IServerDiscordAuthManager, DiscordAuthManager>();
            IoCManager.Register<Content.Corvax.Interfaces.Server.IServerJoinQueueManager, JoinQueueManager>();
            // Alteros-Sponsor

            IoCManager.Register<IServerVPNGuardManager, VpnGuardFile>(); // c4llv07e vpn ban
            IoCManager.Register<AdventureConfigManager>(); // c4llv07e config manager
        }
    }
}
