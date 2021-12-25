using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar
{
    // ReSharper disable once InconsistentNaming
    [CVarDefs]
    public sealed class CCVars : CVars
    {
        /*
         * Server
         */

        /// <summary>
        ///     Change this to have the changelog and rules "last seen" date stored separately.
        /// </summary>
        public static readonly CVarDef<string> ServerId =
            CVarDef.Create("server.id", "unknown_server_id", CVar.REPLICATED | CVar.SERVER);

        /*
         * Ambience
         */

        /// <summary>
        ///     Whether the basic 'hum' ambience will be enabled.
        /// </summary>
        public static readonly CVarDef<bool> AmbienceBasicEnabled =
            CVarDef.Create("ambience.basic_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// How long we'll wait until re-sampling nearby objects for ambience.
        /// </summary>
        public static readonly CVarDef<float> AmbientCooldown =
            CVarDef.Create("ambience.cooldown", 0.5f, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<float> AmbientRange =
            CVarDef.Create("ambience.range", 5f, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> MaxAmbientSources =
            CVarDef.Create("ambience.max_sounds", 6, CVar.REPLICATED | CVar.SERVER);

        /*
         * Status
         */

        public static readonly CVarDef<string> StatusMoMMIUrl =
            CVarDef.Create("status.mommiurl", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> StatusMoMMIPassword =
            CVarDef.Create("status.mommipassword", "", CVar.SERVERONLY);


        /*
         * Game
         */

        /// <summary>
        ///     Controls if the game should run station events
        /// </summary>
        public static readonly CVarDef<bool>
            EventsEnabled = CVarDef.Create("events.enabled", true, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool>
            GameDummyTicker = CVarDef.Create("game.dummyticker", false, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Controls if the lobby is enabled. If it is not, and there are no available jobs, you may get stuck on a black screen.
        /// </summary>
        public static readonly CVarDef<bool>
            GameLobbyEnabled = CVarDef.Create("game.lobbyenabled", false, CVar.ARCHIVE);

        /// <summary>
        ///     Controls the duration of the lobby timer in seconds. Defaults to 2 minutes and 30 seconds.
        /// </summary>
        public static readonly CVarDef<int>
            GameLobbyDuration = CVarDef.Create("game.lobbyduration", 150, CVar.ARCHIVE);

        /// <summary>
        ///     Controls if players can latejoin at all.
        /// </summary>
        public static readonly CVarDef<bool>
            GameDisallowLateJoins = CVarDef.Create("game.disallowlatejoins", false, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Controls the default game preset.
        /// </summary>
        public static readonly CVarDef<string>
            GameLobbyDefaultPreset = CVarDef.Create("game.defaultpreset", "suspicion", CVar.ARCHIVE);

        /// <summary>
        ///     Controls if the game can force a different preset if the current preset's criteria are not met.
        /// </summary>
        public static readonly CVarDef<bool>
            GameLobbyFallbackEnabled = CVarDef.Create("game.fallbackenabled", true, CVar.ARCHIVE);

        /// <summary>
        ///     The preset for the game to fall back to if the selected preset could not be used, and fallback is enabled.
        /// </summary>
        public static readonly CVarDef<string>
            GameLobbyFallbackPreset = CVarDef.Create("game.fallbackpreset", "sandbox", CVar.ARCHIVE);

        /// <summary>
        ///     Controls if people can win the game in Suspicion or Deathmatch.
        /// </summary>
        public static readonly CVarDef<bool>
            GameLobbyEnableWin = CVarDef.Create("game.enablewin", true, CVar.ARCHIVE);

        /// <summary>
        ///     Controls the maximum number of character slots a player is allowed to have.
        /// </summary>
        public static readonly CVarDef<int>
            GameMaxCharacterSlots = CVarDef.Create("game.maxcharacterslots", 10, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Controls the game map prototype to load. SS14 stores these prototypes in Prototypes/Maps.
        /// </summary>
        public static readonly CVarDef<string>
            GameMap = CVarDef.Create("game.map", "saltern", CVar.SERVERONLY);

        /// <summary>
        ///     Controls if the game should obey map criteria or not. Overriden if a map vote or similar occurs.
        /// </summary>
        public static readonly CVarDef<bool>
            GameMapForced = CVarDef.Create("game.mapforced", false, CVar.SERVERONLY);

        /// <summary>
        ///     Whether a random position offset will be applied to the station on roundstart.
        /// </summary>
        public static readonly CVarDef<bool> StationOffset =
            CVarDef.Create("game.station_offset", true);

        /// <summary>
        /// When the default blueprint is loaded what is the maximum amount it can be offset from 0,0.
        /// Does nothing without <see cref="StationOffset"/> as true.
        /// </summary>
        public static readonly CVarDef<float> MaxStationOffset =
            CVarDef.Create("game.maxstationoffset", 1000.0f);

        /// <summary>
        ///     Whether a random rotation will be applied to the station on roundstart.
        /// </summary>
        public static readonly CVarDef<bool> StationRotation =
            CVarDef.Create("game.station_rotation", true);

        /// <summary>
        ///     When enabled, guests will be assigned permanent UIDs and will have their preferences stored.
        /// </summary>
        public static readonly CVarDef<bool> GamePersistGuests =
            CVarDef.Create("game.persistguests", true, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool> GameDiagonalMovement =
            CVarDef.Create("game.diagonalmovement", true, CVar.ARCHIVE);

        public static readonly CVarDef<int> SoftMaxPlayers =
            CVarDef.Create("game.soft_max_players", 30, CVar.SERVERONLY | CVar.ARCHIVE);

        /*
         * Discord
         */

        public static readonly CVarDef<string> DiscordAHelpWebhook =
            CVarDef.Create("discord.ahelp_webhook", string.Empty, CVar.SERVERONLY);

        /*
         * Suspicion
         */

        public static readonly CVarDef<int> SuspicionMinPlayers =
            CVarDef.Create("suspicion.min_players", 5);

        public static readonly CVarDef<int> SuspicionMinTraitors =
            CVarDef.Create("suspicion.min_traitors", 2);

        public static readonly CVarDef<int> SuspicionPlayersPerTraitor =
            CVarDef.Create("suspicion.players_per_traitor", 5);

        public static readonly CVarDef<int> SuspicionStartingBalance =
            CVarDef.Create("suspicion.starting_balance", 20);

        public static readonly CVarDef<int> SuspicionMaxTimeSeconds =
            CVarDef.Create("suspicion.max_time_seconds", 300);

        /*
         * Traitor
         */

        public static readonly CVarDef<int> TraitorMinPlayers =
            CVarDef.Create("traitor.min_players", 5);

        public static readonly CVarDef<int> TraitorMaxTraitors =
            CVarDef.Create("traitor.max_traitors", 7);

        public static readonly CVarDef<int> TraitorPlayersPerTraitor =
            CVarDef.Create("traitor.players_per_traitor", 5);

        public static readonly CVarDef<int> TraitorCodewordCount =
            CVarDef.Create("traitor.codeword_count", 4);

        public static readonly CVarDef<int> TraitorStartingBalance =
            CVarDef.Create("traitor.starting_balance", 20);

        public static readonly CVarDef<int> TraitorMaxDifficulty =
            CVarDef.Create("traitor.max_difficulty", 6);

        public static readonly CVarDef<int> TraitorMaxPicks =
            CVarDef.Create("traitor.max_picks", 20);

        /*
         * TraitorDeathMatch
         */

        public static readonly CVarDef<int> TraitorDeathMatchStartingBalance =
            CVarDef.Create("traitordm.starting_balance", 20);

        /*
         * Console
         */

        public static readonly CVarDef<bool>
            ConsoleLoginLocal = CVarDef.Create("console.loginlocal", true, CVar.ARCHIVE | CVar.SERVERONLY);


        /*
         * Database stuff
         */

        public static readonly CVarDef<string> DatabaseEngine =
            CVarDef.Create("database.engine", "sqlite", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabaseSqliteDbPath =
            CVarDef.Create("database.sqlite_dbpath", "preferences.db", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgHost =
            CVarDef.Create("database.pg_host", "localhost", CVar.SERVERONLY);

        public static readonly CVarDef<int> DatabasePgPort =
            CVarDef.Create("database.pg_port", 5432, CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgDatabase =
            CVarDef.Create("database.pg_database", "ss14", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgUsername =
            CVarDef.Create("database.pg_username", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgPassword =
            CVarDef.Create("database.pg_password", "", CVar.SERVERONLY);

        // Basically only exists for integration tests to avoid race conditions.
        public static readonly CVarDef<bool> DatabaseSynchronous =
            CVarDef.Create("database.sync", false, CVar.SERVERONLY);


        /*
         * Outline
         */

        public static readonly CVarDef<bool> OutlineEnabled =
            CVarDef.Create("outline.enabled", true, CVar.CLIENTONLY);


        /*
         * Parallax
         */

        public static readonly CVarDef<bool> ParallaxEnabled =
            CVarDef.Create("parallax.enabled", true, CVar.CLIENTONLY);

        public static readonly CVarDef<bool> ParallaxDebug =
            CVarDef.Create("parallax.debug", false, CVar.CLIENTONLY);

        /*
         * Physics
         */

        /// <summary>
        /// When a mob is walking should its X / Y movement be relative to its parent (true) or the map (false).
        /// </summary>
        public static readonly CVarDef<bool> RelativeMovement =
            CVarDef.Create("physics.relative_movement", true, CVar.ARCHIVE | CVar.REPLICATED);

        public static readonly CVarDef<float> TileFrictionModifier =
            CVarDef.Create("physics.tile_friction", 40.0f);

        public static readonly CVarDef<float> StopSpeed =
            CVarDef.Create("physics.stop_speed", 0.1f);

        /// <summary>
        /// Whether mobs can push objects like lockers.
        /// </summary>
        /// <remarks>
        /// Technically client doesn't need to know about it but this may prevent a bug in the distant future so it stays.
        /// </remarks>
        public static readonly CVarDef<bool> MobPushing =
            CVarDef.Create("physics.mob_pushing", true, CVar.REPLICATED);

        /*
         * Lobby music
         */

        public static readonly CVarDef<bool> LobbyMusicEnabled =
            CVarDef.Create("ambience.lobbymusicenabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * HUD
         */

        public static readonly CVarDef<int> HudTheme =
            CVarDef.Create("hud.theme", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> HudHeldItemShow =
            CVarDef.Create("hud.held_item_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<float> HudHeldItemOffset =
            CVarDef.Create("hud.held_item_offset", 28f, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> HudFpsCounterVisible =
            CVarDef.Create("hud.fps_counter_visible", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * NPCs
         */

        public static readonly CVarDef<int> NPCMaxUpdates =
            CVarDef.Create("npc.max_updates", 64);

        public static readonly CVarDef<bool> NPCEnabled = CVarDef.Create("npc.enabled", true);

        /*
         * Net
         */

        public static readonly CVarDef<float> NetAtmosDebugOverlayTickRate =
            CVarDef.Create("net.atmosdbgoverlaytickrate", 3.0f);

        public static readonly CVarDef<float> NetGasOverlayTickRate =
            CVarDef.Create("net.gasoverlaytickrate", 3.0f);

        public static readonly CVarDef<int> GasOverlayThresholds =
            CVarDef.Create("net.gasoverlaythresholds", 20);

        /*
         * Admin stuff
         */

        public static readonly CVarDef<bool> AdminAnnounceLogin =
            CVarDef.Create("admin.announce_login", true, CVar.SERVERONLY);

        public static readonly CVarDef<bool> AdminAnnounceLogout =
            CVarDef.Create("admin.announce_logout", true, CVar.SERVERONLY);

        /*
         * Admin logs
         */

        /// <summary>
        ///     Controls if admin logs are enabled. Highly recommended to shut this off for development.
        /// </summary>
        public static readonly CVarDef<bool> AdminLogsEnabled =
            CVarDef.Create("adminlogs.enabled", true, CVar.SERVERONLY);

        public static readonly CVarDef<float> AdminLogsQueueSendDelay =
            CVarDef.Create("adminlogs.queue_send_delay_seconds", 5f, CVar.SERVERONLY);

        public static readonly CVarDef<int> AdminLogsQueueMax =
            CVarDef.Create("adminlogs.queue_max", 5000, CVar.SERVERONLY);

        public static readonly CVarDef<int> AdminLogsPreRoundQueueMax =
            CVarDef.Create("adminlogs.pre_round_queue_max", 5000, CVar.SERVERONLY);

        // How many logs to send to the client at once
        public static readonly CVarDef<int> AdminLogsClientBatchSize =
            CVarDef.Create("adminlogs.client_batch_size", 1000, CVar.SERVERONLY);

        /*
         * Atmos
         */

        /// <summary>
        ///     Whether gas differences will move entities.
        /// </summary>
        public static readonly CVarDef<bool> SpaceWind =
            CVarDef.Create("atmos.space_wind", true, CVar.SERVERONLY);

        /// <summary>
        ///     The sound that plays when space wind occurs.
        /// </summary>
        public static readonly CVarDef<string> SpaceWindSound =
            CVarDef.Create("atmos.space_wind_sound", "/Audio/Effects/space_wind.ogg", CVar.SERVERONLY);

        /// <summary>
        ///     Whether monstermos tile equalization is enabled.
        /// </summary>
        public static readonly CVarDef<bool> MonstermosEqualization =
            CVarDef.Create("atmos.monstermos_equalization", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether monstermos explosive depressurization is enabled.
        ///     Needs <see cref="MonstermosEqualization"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> MonstermosDepressurization =
            CVarDef.Create<bool>("atmos.monstermos_depressurization", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether monstermos explosive depressurization will rip tiles..
        ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> MonstermosRipTiles =
            CVarDef.Create<bool>("atmos.monstermos_rip_tiles", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether explosive depressurization will cause the grid to gain an impulse.
        ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> AtmosGridImpulse =
            CVarDef.Create("atmos.grid_impulse", false, CVar.SERVERONLY);

        /// <summary>
        ///     Whether atmos superconduction is enabled.
        /// </summary>
        /// <remarks> Disabled by default, superconduction is awful. </remarks>
        public static readonly CVarDef<bool> Superconduction =
            CVarDef.Create("atmos.superconduction", false, CVar.SERVERONLY);

        /// <summary>
        ///     Whether excited groups will be processed and created.
        /// </summary>
        public static readonly CVarDef<bool> ExcitedGroups =
            CVarDef.Create("atmos.excited_groups", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether all tiles in an excited group will clear themselves once being exposed to space.
        ///     Similar to <see cref="MonstermosDepressurization"/>, without none of the tile ripping or
        ///     things being thrown around very violently.
        ///     Needs <see cref="ExcitedGroups"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> ExcitedGroupsSpaceIsAllConsuming =
            CVarDef.Create("atmos.excited_groups_space_is_all_consuming", false, CVar.SERVERONLY);

        /// <summary>
        ///     Maximum time in milliseconds that atmos can take processing.
        /// </summary>
        public static readonly CVarDef<float> AtmosMaxProcessTime =
            CVarDef.Create("atmos.max_process_time", 5f, CVar.SERVERONLY);

        /// <summary>
        ///     Atmos tickrate in TPS. Atmos processing will happen every 1/TPS seconds.
        /// </summary>
        public static readonly CVarDef<float> AtmosTickRate =
            CVarDef.Create("atmos.tickrate", 26f, CVar.SERVERONLY);

        /*
         * MIDI instruments
         */

        public static readonly CVarDef<int> MaxMidiEventsPerSecond =
            CVarDef.Create("midi.max_events_per_second", 1000, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> MaxMidiEventsPerBatch =
            CVarDef.Create("midi.max_events_per_batch", 60, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> MaxMidiBatchesDropped =
            CVarDef.Create("midi.max_batches_dropped", 1, CVar.SERVERONLY);

        public static readonly CVarDef<int> MaxMidiLaggedBatches =
            CVarDef.Create("midi.max_lagged_batches", 8, CVar.SERVERONLY);

        /*
         * Holidays
         */

        public static readonly CVarDef<bool> HolidaysEnabled = CVarDef.Create("holidays.enabled", true, CVar.SERVERONLY);

        /*
         * Branding stuff
         */

        public static readonly CVarDef<bool> BrandingSteam = CVarDef.Create("branding.steam", false, CVar.CLIENTONLY);

        /*
         * OOC
         */

        public static readonly CVarDef<bool> OocEnabled = CVarDef.Create("ooc.enabled", true, CVar.NOTIFY);

        public static readonly CVarDef<bool> AdminOocEnabled =
            CVarDef.Create("ooc.enabled_admin", true, CVar.NOTIFY);

        /*
         * Entity Menu Grouping Types
         */
        public static readonly CVarDef<int> EntityMenuGroupingType = CVarDef.Create("entity_menu", 0, CVar.CLIENTONLY);

        /*
         * VOTE
         */

        /// <summary>
        ///     Allows enabling/disabling player-started votes for ultimate authority
        /// </summary>
        public static readonly CVarDef<bool> VoteEnabled =
            CVarDef.Create("vote.enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to restart votes
        /// </summary>
        public static readonly CVarDef<bool> VoteRestartEnabled =
            CVarDef.Create("vote.restart_enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to preset votes
        /// </summary>
        public static readonly CVarDef<bool> VotePresetEnabled =
            CVarDef.Create("vote.preset_enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to map votes
        /// </summary>
        public static readonly CVarDef<bool> VoteMapEnabled =
            CVarDef.Create("vote.map_enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     The required ratio of the server that must agree for a restart round vote to go through.
        /// </summary>
        public static readonly CVarDef<float> VoteRestartRequiredRatio =
            CVarDef.Create("vote.restart_required_ratio", 0.8f, CVar.SERVERONLY);

        /// <summary>
        ///     The delay which two votes of the same type are allowed to be made by separate people, in seconds.
        /// </summary>
        public static readonly CVarDef<float> VoteSameTypeTimeout =
            CVarDef.Create("vote.same_type_timeout", 240f, CVar.SERVERONLY);

        /*
         * BAN
         */

        public static readonly CVarDef<bool> BanHardwareIds =
            CVarDef.Create("ban.hardware_ids", true, CVar.SERVERONLY);

        /*
         * Shuttles
         */
        // Once cruising actually gets implemented I'd likely drop this speed to 3 maybe.
        public static readonly CVarDef<float> ShuttleDockSpeedCap =
            CVarDef.Create("shuttle.dock_speed_cap", 5f, CVar.SERVERONLY);

        /*
         * VIEWPORT
         */

        public static readonly CVarDef<bool> ViewportStretch =
            CVarDef.Create("viewport.stretch", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> ViewportFixedScaleFactor =
            CVarDef.Create("viewport.fixed_scale_factor", 2, CVar.CLIENTONLY | CVar.ARCHIVE);

        // This default is basically specifically chosen so fullscreen/maximized 1080p hits a 2x snap and does NN.
        public static readonly CVarDef<int> ViewportSnapToleranceMargin =
            CVarDef.Create("viewport.snap_tolerance_margin", 64, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> ViewportSnapToleranceClip =
            CVarDef.Create("viewport.snap_tolerance_clip", 32, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> ViewportScaleRender =
            CVarDef.Create("viewport.scale_render", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * CHAT
         */

        public static readonly CVarDef<int> ChatMaxMessageLength =
            CVarDef.Create("chat.max_message_length", 1000, CVar.SERVER | CVar.REPLICATED);

        public static readonly CVarDef<bool> ChatSanitizerEnabled =
            CVarDef.Create("chat.chat_sanitizer_enabled", true, CVar.SERVERONLY);

        /*
         * AFK
         */

        /// <summary>
        /// How long a client can go without any input before being considered AFK.
        /// </summary>
        public static readonly CVarDef<float> AfkTime =
            CVarDef.Create("afk.time", 60f, CVar.SERVERONLY);

        /*
         * IC
         */

        /// <summary>
        /// Restricts IC character names to alphanumeric chars.
        /// </summary>
        public static readonly CVarDef<bool> RestrictedNames =
            CVarDef.Create("ic.restricted_names", true, CVar.SERVER | CVar.REPLICATED);

        /*
         * Salvage
         */

        /// <summary>
        ///     Forced salvage map prototype name (if empty, randomly selected)
        /// </summary>
        public static readonly CVarDef<string>
            SalvageForced = CVarDef.Create("salvage.forced", "", CVar.SERVERONLY);

        /*
         * Rules
         */

        /// <summary>
        /// Time that players have to wait before rules can be accepted.
        /// </summary>
        public static readonly CVarDef<float> RulesWaitTime =
            CVarDef.Create("rules.time", 45f, CVar.SERVER | CVar.REPLICATED);
    }
}
