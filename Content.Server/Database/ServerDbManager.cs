﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using LogLevel = Robust.Shared.Log.LogLevel;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Content.Server.Database
{
    public interface IServerDbManager
    {
        void Init();

        #region Preferences
        Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile);
        Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index);

        Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot);

        Task SaveAdminOOCColorAsync(NetUserId userId, Color color);

        // Single method for two operations for transaction.
        Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot);
        Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId);
        #endregion

        #region User Ids
        // Username assignment (for guest accounts, so they persist GUID)
        Task AssignUserIdAsync(string name, NetUserId userId);
        Task<NetUserId?> GetAssignedUserIdAsync(string name);
        #endregion

        #region Bans
        /// <summary>
        ///     Looks up a ban by id.
        ///     This will return a pardoned ban as well.
        /// </summary>
        /// <param name="id">The ban id to look for.</param>
        /// <returns>The ban with the given id or null if none exist.</returns>
        Task<ServerBanDef?> GetServerBanAsync(int id);

        /// <summary>
        ///     Looks up an user's most recent received un-pardoned ban.
        ///     This will NOT return a pardoned ban.
        ///     One of <see cref="address"/> or <see cref="userId"/> need to not be null.
        /// </summary>
        /// <param name="address">The ip address of the user.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="hwId">The hardware ID of the user.</param>
        /// <returns>The user's latest received un-pardoned ban, or null if none exist.</returns>
        Task<ServerBanDef?> GetServerBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId);

        /// <summary>
        ///     Looks up an user's ban history.
        ///     One of <see cref="address"/> or <see cref="userId"/> need to not be null.
        /// </summary>
        /// <param name="address">The ip address of the user.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="hwId">The HWId of the user.</param>
        /// <param name="includeUnbanned">If true, bans that have been expired or pardoned are also included.</param>
        /// <returns>The user's ban history.</returns>
        Task<List<ServerBanDef>> GetServerBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned=true);

        Task AddServerBanAsync(ServerBanDef serverBan);
        Task AddServerUnbanAsync(ServerUnbanDef serverBan);
        #endregion

        #region Role Bans
        /// <summary>
        ///     Looks up a role ban by id.
        ///     This will return a pardoned role ban as well.
        /// </summary>
        /// <param name="id">The role ban id to look for.</param>
        /// <returns>The role ban with the given id or null if none exist.</returns>
        Task<ServerRoleBanDef?> GetServerRoleBanAsync(int id);

        /// <summary>
        ///     Looks up an user's role ban history.
        ///     This will return pardoned role bans based on the <see cref="includeUnbanned"/> bool.
        ///     Requires one of <see cref="address"/>, <see cref="userId"/>, or <see cref="hwId"/> to not be null.
        /// </summary>
        /// <param name="address">The IP address of the user.</param>
        /// <param name="userId">The NetUserId of the user.</param>
        /// <param name="hwId">The Hardware Id of the user.</param>
        /// <param name="includeUnbanned">Whether expired and pardoned bans are included.</param>
        /// <returns>The user's role ban history.</returns>
        Task<List<ServerRoleBanDef>> GetServerRoleBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned = true);

        Task AddServerRoleBanAsync(ServerRoleBanDef serverBan);
        Task AddServerRoleUnbanAsync(ServerRoleUnbanDef serverBan);
        #endregion

        #region Player Records
        Task UpdatePlayerRecordAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId);
        Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel = default);
        Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel = default);
        #endregion

        #region Connection Logs
        /// <returns>ID of newly inserted connection log row.</returns>
        Task<int> AddConnectionLogAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId,
            ConnectionDenyReason? denied);

        Task AddServerBanHitsAsync(int connection, IEnumerable<ServerBanDef> bans);

        #endregion

        #region Admin Ranks
        Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel = default);
        Task<AdminRank?> GetAdminRankAsync(int id, CancellationToken cancel = default);

        Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel = default);

        Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel = default);
        Task AddAdminAsync(Admin admin, CancellationToken cancel = default);
        Task UpdateAdminAsync(Admin admin, CancellationToken cancel = default);

        Task RemoveAdminRankAsync(int rankId, CancellationToken cancel = default);
        Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel = default);
        Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel = default);
        #endregion

        #region Rounds

        Task<int> AddNewRound(Server server, params Guid[] playerIds);
        Task<Round> GetRound(int id);
        Task AddRoundPlayers(int id, params Guid[] playerIds);

        #endregion

        #region Admin Logs

        Task<Server> AddOrGetServer(string serverName);
        Task AddAdminLogs(List<QueuedLog> logs);
        IAsyncEnumerable<string> GetAdminLogMessages(LogFilter? filter = null);
        IAsyncEnumerable<SharedAdminLog> GetAdminLogs(LogFilter? filter = null);
        IAsyncEnumerable<JsonDocument> GetAdminLogsJson(LogFilter? filter = null);

        #endregion

        #region Whitelist

        Task<bool> GetWhitelistStatusAsync(NetUserId player);

        Task AddToWhitelistAsync(NetUserId player);

        Task RemoveFromWhitelistAsync(NetUserId player);

        #endregion

        #region Uploaded Resources Logs

        Task AddUploadedResourceLogAsync(NetUserId user, DateTime date, string path, byte[] data);

        Task PurgeUploadedResourceLogAsync(int days);

        #endregion
    }

    public sealed class ServerDbManager : IServerDbManager
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly ILogManager _logMgr = default!;

        private ServerDbBase _db = default!;
        private LoggingProvider _msLogProvider = default!;
        private ILoggerFactory _msLoggerFactory = default!;


        public void Init()
        {
            _msLogProvider = new LoggingProvider(_logMgr);
            _msLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(_msLogProvider);
            });

            var engine = _cfg.GetCVar(CCVars.DatabaseEngine).ToLower();
            switch (engine)
            {
                case "sqlite":
                    var sqliteOptions = CreateSqliteOptions();
                    _db = new ServerDbSqlite(sqliteOptions);
                    break;
                case "postgres":
                    var pgOptions = CreatePostgresOptions();
                    _db = new ServerDbPostgres(pgOptions);
                    break;
                default:
                    throw new InvalidDataException($"Unknown database engine {engine}.");
            }
        }

        public Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {
            return _db.InitPrefsAsync(userId, defaultProfile);
        }

        public Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            return _db.SaveSelectedCharacterIndexAsync(userId, index);
        }

        public Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
        {
            return _db.SaveCharacterSlotAsync(userId, profile, slot);
        }

        public Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot)
        {
            return _db.DeleteSlotAndSetSelectedIndex(userId, deleteSlot, newSlot);
        }

        public Task SaveAdminOOCColorAsync(NetUserId userId, Color color)
        {
            return _db.SaveAdminOOCColorAsync(userId, color);
        }

        public Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId)
        {
            return _db.GetPlayerPreferencesAsync(userId);
        }

        public Task AssignUserIdAsync(string name, NetUserId userId)
        {
            return _db.AssignUserIdAsync(name, userId);
        }

        public Task<NetUserId?> GetAssignedUserIdAsync(string name)
        {
            return _db.GetAssignedUserIdAsync(name);
        }

        public Task<ServerBanDef?> GetServerBanAsync(int id)
        {
            return _db.GetServerBanAsync(id);
        }

        public Task<ServerBanDef?> GetServerBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId)
        {
            return _db.GetServerBanAsync(address, userId, hwId);
        }

        public Task<List<ServerBanDef>> GetServerBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned=true)
        {
            return _db.GetServerBansAsync(address, userId, hwId, includeUnbanned);
        }

        public Task AddServerBanAsync(ServerBanDef serverBan)
        {
            return _db.AddServerBanAsync(serverBan);
        }

        public Task AddServerUnbanAsync(ServerUnbanDef serverUnban)
        {
            return _db.AddServerUnbanAsync(serverUnban);
        }

        #region Role Ban
        public Task<ServerRoleBanDef?> GetServerRoleBanAsync(int id)
        {
            return _db.GetServerRoleBanAsync(id);
        }

        public Task<List<ServerRoleBanDef>> GetServerRoleBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned = true)
        {
            return _db.GetServerRoleBansAsync(address, userId, hwId, includeUnbanned);
        }

        public Task AddServerRoleBanAsync(ServerRoleBanDef serverRoleBan)
        {
            return _db.AddServerRoleBanAsync(serverRoleBan);
        }

        public Task AddServerRoleUnbanAsync(ServerRoleUnbanDef serverRoleUnban)
        {
            return _db.AddServerRoleUnbanAsync(serverRoleUnban);
        }
        #endregion

        public Task UpdatePlayerRecordAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId)
        {
            return _db.UpdatePlayerRecord(userId, userName, address, hwId);
        }

        public Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel = default)
        {
            return _db.GetPlayerRecordByUserName(userName, cancel);
        }

        public Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel = default)
        {
            return _db.GetPlayerRecordByUserId(userId, cancel);
        }

        public Task<int> AddConnectionLogAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId,
            ConnectionDenyReason? denied)
        {
            return _db.AddConnectionLogAsync(userId, userName, address, hwId, denied);
        }

        public Task AddServerBanHitsAsync(int connection, IEnumerable<ServerBanDef> bans)
        {
            return _db.AddServerBanHitsAsync(connection, bans);
        }

        public Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel = default)
        {
            return _db.GetAdminDataForAsync(userId, cancel);
        }

        public Task<AdminRank?> GetAdminRankAsync(int id, CancellationToken cancel = default)
        {
            return _db.GetAdminRankDataForAsync(id, cancel);
        }

        public Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel = default)
        {
            return _db.GetAllAdminAndRanksAsync(cancel);
        }

        public Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel = default)
        {
            return _db.RemoveAdminAsync(userId, cancel);
        }

        public Task AddAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            return _db.AddAdminAsync(admin, cancel);
        }

        public Task UpdateAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            return _db.UpdateAdminAsync(admin, cancel);
        }

        public Task RemoveAdminRankAsync(int rankId, CancellationToken cancel = default)
        {
            return _db.RemoveAdminRankAsync(rankId, cancel);
        }

        public Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            return _db.AddAdminRankAsync(rank, cancel);
        }

        public Task<int> AddNewRound(Server server, params Guid[] playerIds)
        {
            return _db.AddNewRound(server, playerIds);
        }

        public Task<Round> GetRound(int id)
        {
            return _db.GetRound(id);
        }

        public Task AddRoundPlayers(int id, params Guid[] playerIds)
        {
            return _db.AddRoundPlayers(id, playerIds);
        }

        public Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            return _db.UpdateAdminRankAsync(rank, cancel);
        }

        public Task<Server> AddOrGetServer(string serverName)
        {
            return _db.AddOrGetServer(serverName);
        }

        public Task AddAdminLogs(List<QueuedLog> logs)
        {
            return _db.AddAdminLogs(logs);
        }

        public IAsyncEnumerable<string> GetAdminLogMessages(LogFilter? filter = null)
        {
            return _db.GetAdminLogMessages(filter);
        }

        public IAsyncEnumerable<SharedAdminLog> GetAdminLogs(LogFilter? filter = null)
        {
            return _db.GetAdminLogs(filter);
        }

        public IAsyncEnumerable<JsonDocument> GetAdminLogsJson(LogFilter? filter = null)
        {
            return _db.GetAdminLogsJson(filter);
        }

        public Task<bool> GetWhitelistStatusAsync(NetUserId player)
        {
            return _db.GetWhitelistStatusAsync(player);
        }

        public Task AddToWhitelistAsync(NetUserId player)
        {
            return _db.AddToWhitelistAsync(player);
        }

        public Task RemoveFromWhitelistAsync(NetUserId player)
        {
            return _db.RemoveFromWhitelistAsync(player);
        }

        public Task AddUploadedResourceLogAsync(NetUserId user, DateTime date, string path, byte[] data)
        {
            return _db.AddUploadedResourceLogAsync(user, date, path, data);
        }

        public Task PurgeUploadedResourceLogAsync(int days)
        {
            return _db.PurgeUploadedResourceLogAsync(days);
        }

        private DbContextOptions<PostgresServerDbContext> CreatePostgresOptions()
        {
            var host = _cfg.GetCVar(CCVars.DatabasePgHost);
            var port = _cfg.GetCVar(CCVars.DatabasePgPort);
            var db = _cfg.GetCVar(CCVars.DatabasePgDatabase);
            var user = _cfg.GetCVar(CCVars.DatabasePgUsername);
            var pass = _cfg.GetCVar(CCVars.DatabasePgPassword);

            var builder = new DbContextOptionsBuilder<PostgresServerDbContext>();
            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = db,
                Username = user,
                Password = pass
            }.ConnectionString;

            Logger.DebugS("db.manager", $"Using Postgres \"{host}:{port}/{db}\"");

            builder.UseNpgsql(connectionString);
            SetupLogging(builder);
            return builder.Options;
        }

        private DbContextOptions<SqliteServerDbContext> CreateSqliteOptions()
        {
            var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();

            var configPreferencesDbPath = _cfg.GetCVar(CCVars.DatabaseSqliteDbPath);
            var inMemory = _res.UserData.RootDir == null;

            SqliteConnection connection;
            if (!inMemory)
            {
                var finalPreferencesDbPath = Path.Combine(_res.UserData.RootDir!, configPreferencesDbPath);
                Logger.DebugS("db.manager", $"Using SQLite DB \"{finalPreferencesDbPath}\"");
                connection = new SqliteConnection($"Data Source={finalPreferencesDbPath}");
            }
            else
            {
                Logger.DebugS("db.manager", $"Using in-memory SQLite DB");
                connection = new SqliteConnection("Data Source=:memory:");
                // When using an in-memory DB we have to open it manually
                // so EFCore doesn't open, close and wipe it.
                connection.Open();
            }

            builder.UseSqlite(connection);
            SetupLogging(builder);
            return builder.Options;
        }

        private void SetupLogging(DbContextOptionsBuilder builder)
        {
            builder.UseLoggerFactory(_msLoggerFactory);
        }

        private sealed class LoggingProvider : ILoggerProvider
        {
            private readonly ILogManager _logManager;

            public LoggingProvider(ILogManager logManager)
            {
                _logManager = logManager;
            }

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new MSLogger(_logManager.GetSawmill("db.ef"));
            }
        }

        private sealed class MSLogger : ILogger
        {
            private readonly ISawmill _sawmill;

            public MSLogger(ISawmill sawmill)
            {
                _sawmill = sawmill;
            }

            public void Log<TState>(MSLogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var lvl = logLevel switch
                {
                    MSLogLevel.Trace => LogLevel.Debug,
                    MSLogLevel.Debug => LogLevel.Debug,
                    // EFCore feels the need to log individual DB commands as "Information" so I'm slapping debug on it.
                    MSLogLevel.Information => LogLevel.Debug,
                    MSLogLevel.Warning => LogLevel.Warning,
                    MSLogLevel.Error => LogLevel.Error,
                    MSLogLevel.Critical => LogLevel.Fatal,
                    MSLogLevel.None => LogLevel.Debug,
                    _ => LogLevel.Debug
                };

                _sawmill.Log(lvl, formatter(state, exception));
            }

            public bool IsEnabled(MSLogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                // TODO: this
                return null!;
            }
        }
    }
}
