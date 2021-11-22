﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Administration.Logs.Converters;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Prometheus;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;

namespace Content.Server.Administration.Logs;

public class AdminLogSystem : SharedAdminLogSystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    [Dependency] private readonly GameTicker _gameTicker = default!;

    public const string SawmillId = "admin.logs";

    private static readonly Histogram DatabaseUpdateTime = Metrics.CreateHistogram(
        "admin_logs_database_time",
        "Time used to send logs to the database in ms",
        new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(0, 0.5, 20)
        });

    private static readonly Gauge QueueCapReached = Metrics.CreateGauge(
        "admin_logs_queue_cap_reached",
        "Number of times the log queue cap has been reached in a round.");

    private static readonly Gauge LogsSent = Metrics.CreateGauge(
        "admin_logs_sent",
        "Amount of logs sent to the database in a round.");

    private static readonly JsonNamingPolicy NamingPolicy = JsonNamingPolicy.CamelCase;

    // Init only
    private ISawmill _sawmill = default!;
    private JsonSerializerOptions _jsonOptions = default!;

    // CVars
    private bool _metricsEnabled;
    private TimeSpan _queueSendDelay;
    private int _queueMax;

    // Per update
    private float _accumulatedFrameTime;
    private readonly ConcurrentQueue<QueuedLog> _logsToAdd = new();

    private int CurrentRoundId => _gameTicker.RoundId;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill(SawmillId);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy
        };

        foreach (var converter in _reflection.FindTypesWithAttribute<AdminLogConverterAttribute>())
        {
            var instance = _typeFactory.CreateInstance<JsonConverter>(converter);
            _jsonOptions.Converters.Add(instance);
        }

        var converterNames = _jsonOptions.Converters.Select(converter => converter.GetType().Name);
        _sawmill.Info($"Admin log converters found: {string.Join(" ", converterNames)}");

        _configuration.OnValueChanged(CVars.MetricsEnabled,
            value => _metricsEnabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsQueueSendDelay,
            value => _queueSendDelay = TimeSpan.FromSeconds(value), true);
        _configuration.OnValueChanged(CCVars.AdminLogsQueueMax,
            value => _queueMax = value, true);

        if (_metricsEnabled)
        {
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }

        SubscribeLocalEvent<RoundStartingEvent>(RoundStarting);
    }

    public override async void Shutdown()
    {
        base.Shutdown();

        if (!_logsToAdd.IsEmpty)
        {
            await SendLogs();
        }
    }

    public override async void Update(float frameTime)
    {
        var count = _logsToAdd.Count;
        if (count == 0)
        {
            return;
        }

        if (count < _queueMax && _accumulatedFrameTime < _queueSendDelay.TotalSeconds)
        {
            _accumulatedFrameTime += frameTime;
            return;
        }

        await SendLogs();
    }

    private async Task SendLogs()
    {
        var copy = new List<QueuedLog>(_logsToAdd);
        _logsToAdd.Clear();
        _accumulatedFrameTime = 0;

        // ship the logs to Azkaban
        var task = Task.Run(() =>
        {
            _db.AddAdminLogs(copy);
        });

        if (_metricsEnabled)
        {
            if (copy.Count >= _queueMax)
            {
                QueueCapReached.Inc();
            }

            LogsSent.Inc(copy.Count);

            using (DatabaseUpdateTime.NewTimer())
            {
                await task;
                return;
            }
        }

        await task;
    }

    private void RoundStarting(RoundStartingEvent ev)
    {
        if (_metricsEnabled)
        {
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }
    }

    public (JsonDocument json, List<Guid> players, List<(int id, string? name)> entities) ToJson(
        Dictionary<string, object?> properties)
    {
        var entities = new List<(int id, string? name)>();
        var players = new List<Guid>();
        var parsed = new Dictionary<string, object?>();

        foreach (var key in properties.Keys)
        {
            var value = properties[key];
            var parsedKey = NamingPolicy.ConvertName(key);
            parsed.Add(parsedKey, value);

            EntityUid? entityId = properties[key] switch
            {
                EntityUid id => id,
                IEntity entity => entity.Uid,
                IPlayerSession {AttachedEntityUid: { }} session => session.AttachedEntityUid.Value,
                IComponent component => component.OwnerUid,
                _ => null
            };

            if (entityId is not { } uid)
            {
                continue;
            }

            var entityName = _entityManager.TryGetEntity(uid, out var resolvedEntity)
                ? resolvedEntity.Name
                : null;

            entities.Add(((int) uid, entityName));

            if (_entityManager.TryGetComponent(uid, out ActorComponent? actor))
            {
                players.Add(actor.PlayerSession.UserId.UserId);
            }
        }

        return (JsonSerializer.SerializeToDocument(parsed, _jsonOptions), players, entities);
    }

    private async void Add(LogType type, LogImpact impact, string message, JsonDocument json, List<Guid> players, List<(int id, string? name)> entities)
    {
        var log = new AdminLog
        {
            RoundId = CurrentRoundId,
            Type = type,
            Impact = impact,
            Date = DateTime.UtcNow,
            Message = message,
            Json = json,
            Players = new List<AdminLogPlayer>(players.Count)
        };

        var queued = new QueuedLog(log, entities);
        _logsToAdd.Enqueue(queued);

        foreach (var id in players)
        {
            var player = new AdminLogPlayer
            {
                PlayerUserId = id,
                RoundId = CurrentRoundId
            };

            log.Players.Add(player);
        }
    }

    public override void Add(LogType type, LogImpact impact, ref LogStringHandler handler)
    {
        var (json, players, entities) = ToJson(handler.Values);
        var message = handler.ToStringAndClear();

        Add(type, impact, message, json, players, entities);
    }

    public override void Add(LogType type, ref LogStringHandler handler)
    {
        Add(type, LogImpact.Medium, ref handler);
    }

    public IAsyncEnumerable<LogRecord> All(LogFilter? filter = null)
    {
        return _db.GetAdminLogs(filter);
    }

    public IAsyncEnumerable<string> AllMessages(LogFilter? filter = null)
    {
        return _db.GetAdminLogMessages(filter);
    }

    public IAsyncEnumerable<JsonDocument> AllJson(LogFilter? filter = null)
    {
        return _db.GetAdminLogsJson(filter);
    }

    public Task<Round> Round(int roundId)
    {
        return _db.GetRound(roundId);
    }

    public IAsyncEnumerable<LogRecord> CurrentRoundLogs(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = CurrentRoundId;
        return All(filter);
    }

    public IAsyncEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = CurrentRoundId;
        return AllMessages(filter);
    }

    public IAsyncEnumerable<JsonDocument> CurrentRoundJson(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = CurrentRoundId;
        return AllJson(filter);
    }

    public Task<Round> CurrentRound()
    {
        return Round(CurrentRoundId);
    }
}
