using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Content.Client.IoC;
using Content.Client.Parallax.Managers;
using Content.IntegrationTests.Tests;
using Content.IntegrationTests.Tests.Destructible;
using Content.IntegrationTests.Tests.DeviceNetwork;
using Content.IntegrationTests.Tests.Interaction.Click;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.UnitTesting;

[assembly: LevelOfParallelism(3)]

namespace Content.IntegrationTests;

/// <summary>
/// Making clients, and servers is slow, this manages a pool of them so tests can reuse them.
/// </summary>
public static class PoolManager
{
    public const string TestMap = "Empty";

    private static readonly (string cvar, string value)[] ServerTestCvars =
    {
        // @formatter:off
        (CCVars.DatabaseSynchronous.Name,     "true"),
        (CCVars.DatabaseSqliteDelay.Name,     "0"),
        (CCVars.HolidaysEnabled.Name,         "false"),
        (CCVars.GameMap.Name,                 TestMap),
        (CCVars.AdminLogsQueueSendDelay.Name, "0"),
        (CVars.NetPVS.Name,                   "false"),
        (CCVars.NPCMaxUpdates.Name,           "999999"),
        (CVars.ThreadParallelCount.Name,      "1"),
        (CCVars.GameRoleTimers.Name,          "false"),
        (CCVars.GridFill.Name,                "false"),
        (CCVars.ArrivalsShuttles.Name,        "false"),
        (CCVars.EmergencyShuttleEnabled.Name, "false"),
        (CCVars.ProcgenPreload.Name,          "false"),
        (CCVars.WorldgenEnabled.Name,         "false"),
        // @formatter:on
    };

    private static int _pairId;
    private static readonly object PairLock = new();

    // Pair, IsBorrowed
    private static readonly Dictionary<Pair, bool> Pairs = new();
    private static bool _dead;
    private static Exception _poolFailureReason;

    private static async Task ConfigurePrototypes(RobustIntegrationTest.IntegrationInstance instance,
        PoolSettings settings)
    {
        await instance.WaitPost(() =>
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var changes = new Dictionary<Type, HashSet<string>>();
            prototypeManager.LoadString(settings.ExtraPrototypes.Trim(), true, changes);
            prototypeManager.ReloadPrototypes(changes);
        });
    }

    private static async Task<(RobustIntegrationTest.ServerIntegrationInstance, PoolTestLogHandler)> GenerateServer(
        PoolSettings poolSettings,
        TextWriter testOut)
    {
        var options = new RobustIntegrationTest.ServerIntegrationOptions
        {
            ExtraPrototypes = poolSettings.ExtraPrototypes,
            ContentStart = true,
            Options = new ServerOptions()
            {
                LoadConfigAndUserData = false,
                LoadContentResources = !poolSettings.NoLoadContent,
            },
            ContentAssemblies = new[]
            {
                typeof(Shared.Entry.EntryPoint).Assembly,
                typeof(Server.Entry.EntryPoint).Assembly,
                typeof(PoolManager).Assembly
            }
        };

        var logHandler = new PoolTestLogHandler("SERVER");
        logHandler.ActivateContext(testOut);
        options.OverrideLogHandler = () => logHandler;

        options.BeforeStart += () =>
        {
            var entSysMan = IoCManager.Resolve<IEntitySystemManager>();
            var compFactory = IoCManager.Resolve<IComponentFactory>();
            entSysMan.LoadExtraSystemType<ResettingEntitySystemTests.TestRoundRestartCleanupEvent>();
            entSysMan.LoadExtraSystemType<InteractionSystemTests.TestInteractionSystem>();
            entSysMan.LoadExtraSystemType<DeviceNetworkTestSystem>();
            entSysMan.LoadExtraSystemType<TestDestructibleListenerSystem>();
            IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
            IoCManager.Resolve<IConfigurationManager>()
                .OnValueChanged(RTCVars.FailureLogLevel, value => logHandler.FailureLevel = value, true);
        };

        SetupCVars(poolSettings, options);

        var server = new RobustIntegrationTest.ServerIntegrationInstance(options);
        await server.WaitIdleAsync();
        return (server, logHandler);
    }

    /// <summary>
    /// This shuts down the pool, and disposes all the server/client pairs.
    /// This is a one time operation to be used when the testing program is exiting.
    /// </summary>
    public static void Shutdown()
    {
        List<Pair> localPairs;
        lock (PairLock)
        {
            if (_dead)
                return;
            _dead = true;
            localPairs = Pairs.Keys.ToList();
        }

        foreach (var pair in localPairs)
        {
            pair.Kill();
        }
    }

    public static string DeathReport()
    {
        lock (PairLock)
        {
            var builder = new StringBuilder();
            var pairs = Pairs.Keys.OrderBy(pair => pair.PairId);
            foreach (var pair in pairs)
            {
                var borrowed = Pairs[pair];
                builder.AppendLine($"Pair {pair.PairId}, Tests Run: {pair.TestHistory.Count}, Borrowed: {borrowed}");
                for (var i = 0; i < pair.TestHistory.Count; i++)
                {
                    builder.AppendLine($"#{i}: {pair.TestHistory[i]}");
                }
            }

            return builder.ToString();
        }
    }

    private static async Task<(RobustIntegrationTest.ClientIntegrationInstance, PoolTestLogHandler)> GenerateClient(
        PoolSettings poolSettings,
        TextWriter testOut)
    {
        var options = new RobustIntegrationTest.ClientIntegrationOptions
        {
            FailureLogLevel = LogLevel.Warning,
            ContentStart = true,
            ExtraPrototypes = poolSettings.ExtraPrototypes,
            ContentAssemblies = new[]
            {
                typeof(Shared.Entry.EntryPoint).Assembly,
                typeof(Client.Entry.EntryPoint).Assembly,
                typeof(PoolManager).Assembly
            }
        };

        if (poolSettings.NoLoadContent)
        {
            Assert.Warn("NoLoadContent does not work on the client, ignoring");
        }

        options.Options = new GameControllerOptions()
        {
            LoadConfigAndUserData = false,
            // LoadContentResources = !poolSettings.NoLoadContent
        };

        var logHandler = new PoolTestLogHandler("CLIENT");
        logHandler.ActivateContext(testOut);
        options.OverrideLogHandler = () => logHandler;

        options.BeforeStart += () =>
        {
            IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
            {
                ClientBeforeIoC = () =>
                {
                    // do not register extra systems or components here -- they will get cleared when the client is
                    // disconnected. just use reflection.
                    IoCManager.Register<IParallaxManager, DummyParallaxManager>(true);
                    IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
                    IoCManager.Resolve<IConfigurationManager>()
                        .OnValueChanged(RTCVars.FailureLogLevel, value => logHandler.FailureLevel = value, true);
                }
            });
        };

        SetupCVars(poolSettings, options);

        var client = new RobustIntegrationTest.ClientIntegrationInstance(options);
        await client.WaitIdleAsync();
        return (client, logHandler);
    }

    private static void SetupCVars(PoolSettings poolSettings, RobustIntegrationTest.IntegrationOptions options)
    {
        foreach (var (cvar, value) in ServerTestCvars)
        {
            options.CVarOverrides[cvar] = value;
        }

        if (poolSettings.DummyTicker)
        {
            options.CVarOverrides[CCVars.GameDummyTicker.Name] = "true";
        }

        options.CVarOverrides[CCVars.GameLobbyEnabled.Name] = poolSettings.InLobby.ToString();

        if (poolSettings.DisableInterpolate)
        {
            options.CVarOverrides[CVars.NetInterp.Name] = "false";
        }

        if (poolSettings.Map != null)
        {
            options.CVarOverrides[CCVars.GameMap.Name] = poolSettings.Map;
        }

        options.CVarOverrides[CCVars.ConfigPresetDevelopment.Name] = "false";

        // This breaks some tests.
        // TODO: Figure out which tests this breaks.
        options.CVarOverrides[CVars.NetBufferSize.Name] = "0";
    }

    /// <summary>
    /// Gets a <see cref="PairTracker"/>, which can be used to get access to a server, and client <see cref="Pair"/>
    /// </summary>
    /// <param name="poolSettings">See <see cref="PoolSettings"/></param>
    /// <returns></returns>
    public static async Task<PairTracker> GetServerClient(PoolSettings poolSettings = null)
    {
        return await GetServerClientPair(poolSettings ?? new PoolSettings());
    }

    private static string GetDefaultTestName(TestContext testContext)
    {
        return testContext.Test.FullName.Replace("Content.IntegrationTests.Tests.", "");
    }

    private static async Task<PairTracker> GetServerClientPair(PoolSettings poolSettings)
    {
        // Trust issues with the AsyncLocal that backs this.
        var testContext = TestContext.CurrentContext;
        var testOut = TestContext.Out;

        DieIfPoolFailure();
        var currentTestName = poolSettings.TestName ?? GetDefaultTestName(testContext);
        var poolRetrieveTimeWatch = new Stopwatch();
        await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Called by test {currentTestName}");
        Pair pair = null;
        try
        {
            poolRetrieveTimeWatch.Start();
            if (poolSettings.MustBeNew)
            {
                await testOut.WriteLineAsync(
                    $"{nameof(GetServerClientPair)}: Creating pair, because settings of pool settings");
                pair = await CreateServerClientPair(poolSettings, testOut);
            }
            else
            {
                await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Looking in pool for a suitable pair");
                pair = GrabOptimalPair(poolSettings);
                if (pair != null)
                {
                    pair.ActivateContext(testOut);

                    await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Suitable pair found");
                    var canSkip = pair.Settings.CanFastRecycle(poolSettings);

                    var cCfg = pair.Client.ResolveDependency<IConfigurationManager>();
                    cCfg.SetCVar(CCVars.NetInterp, !poolSettings.DisableInterpolate);

                    if (canSkip)
                    {
                        ValidateFastRecycle(pair);
                        await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Cleanup not needed, Skipping cleanup of pair");
                    }
                    else
                    {
                        await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Cleaning existing pair");
                        await CleanPooledPair(poolSettings, pair, testOut);
                    }

                    // Ensure client is 1 tick ahead of server? I don't think theres a real reason for why it should be
                    // 1 tick specifically, I am just ensuring consistency with CreateServerClientPair()
                    if (!pair.Settings.NotConnected)
                        await SyncTicks(pair, targetDelta: 1);
                }
                else
                {
                    await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Creating a new pair, no suitable pair found in pool");
                    pair = await CreateServerClientPair(poolSettings, testOut);
                }
            }

        }
        finally
        {
            if (pair != null && pair.TestHistory.Count > 1)
            {
                await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Pair {pair.PairId} Test History Start");
                for (var i = 0; i < pair.TestHistory.Count; i++)
                {
                    await testOut.WriteLineAsync($"- Pair {pair.PairId} Test #{i}: {pair.TestHistory[i]}");
                }
                await testOut.WriteLineAsync($"{nameof(GetServerClientPair)}: Pair {pair.PairId} Test History End");
            }
        }
        var poolRetrieveTime = poolRetrieveTimeWatch.Elapsed;
        await testOut.WriteLineAsync(
            $"{nameof(GetServerClientPair)}: Retrieving pair {pair.PairId} from pool took {poolRetrieveTime.TotalMilliseconds} ms");
        await testOut.WriteLineAsync(
            $"{nameof(GetServerClientPair)}: Returning pair {pair.PairId}");
        pair.Settings = poolSettings;
        pair.TestHistory.Add(currentTestName);
        var usageWatch = new Stopwatch();
        usageWatch.Start();

        return new PairTracker(testOut)
        {
            Pair = pair,
            UsageWatch = usageWatch
        };
    }

    private static void ValidateFastRecycle(Pair pair)
    {
        if (pair.Settings.NoClient || pair.Settings.NoServer)
            return;

        var baseClient = pair.Client.ResolveDependency<IBaseClient>();
        var netMan = pair.Client.ResolveDependency<INetManager>();
        Assert.That(netMan.IsConnected, Is.Not.EqualTo(pair.Settings.NotConnected));

        if (pair.Settings.NotConnected)
            return;

        Assert.That(baseClient.RunLevel, Is.EqualTo(ClientRunLevel.InGame));

        var cPlayer = pair.Client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
        var sPlayer = pair.Server.ResolveDependency<IPlayerManager>();
        Assert.That(sPlayer.Sessions.Count(), Is.EqualTo(1));
        Assert.That(cPlayer.LocalPlayer?.Session?.UserId, Is.EqualTo(sPlayer.Sessions.Single().UserId));

        var ticker = pair.Server.ResolveDependency<EntityManager>().System<GameTicker>();
        Assert.That(ticker.DummyTicker, Is.EqualTo(pair.Settings.DummyTicker));

        var status = ticker.PlayerGameStatuses[sPlayer.Sessions.Single().UserId];
        var expected = pair.Settings.InLobby
            ? PlayerGameStatus.NotReadyToPlay
            : PlayerGameStatus.JoinedGame;

        Assert.That(status, Is.EqualTo(expected));
    }

    private static Pair GrabOptimalPair(PoolSettings poolSettings)
    {
        lock (PairLock)
        {
            Pair fallback = null;
            foreach (var pair in Pairs.Keys)
            {
                if (Pairs[pair])
                    continue;
                if (!pair.Settings.CanFastRecycle(poolSettings))
                {
                    fallback = pair;
                    continue;
                }
                Pairs[pair] = true;
                return pair;
            }

            if (fallback != null)
            {
                Pairs[fallback!] = true;
            }
            return fallback;
        }
    }

    /// <summary>
    /// Used by PairTracker after checking the server/client pair, Don't use this.
    /// </summary>
    /// <param name="pair"></param>
    public static void NoCheckReturn(Pair pair)
    {
        lock (PairLock)
        {
            if (pair.Dead)
            {
                Pairs.Remove(pair);
            }
            else
            {
                Pairs[pair] = false;
            }
        }
    }

    private static async Task CleanPooledPair(PoolSettings poolSettings, Pair pair, TextWriter testOut)
    {
        var methodWatch = new Stopwatch();
        methodWatch.Start();
        await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Setting CVar ");
        var configManager = pair.Server.ResolveDependency<IConfigurationManager>();
        var entityManager = pair.Server.ResolveDependency<IEntityManager>();
        var gameTicker = entityManager.System<GameTicker>();

        configManager.SetCVar(CCVars.GameLobbyEnabled, poolSettings.InLobby);
        configManager.SetCVar(CCVars.GameMap, TestMap);

        var cNetMgr = pair.Client.ResolveDependency<IClientNetManager>();
        if (!cNetMgr.IsConnected)
        {
            await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Connecting client, and restarting server");
            pair.Client.SetConnectTarget(pair.Server);
            await pair.Server.WaitPost(() =>
            {
                gameTicker.RestartRound();
            });
            await pair.Client.WaitPost(() =>
            {
                cNetMgr.ClientConnect(null!, 0, null!);
            });
        }
        await ReallyBeIdle(pair, 11);

        await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Disconnecting client, and restarting server");

        await pair.Client.WaitPost(() =>
        {
            cNetMgr.ClientDisconnect("Test pooling cleanup disconnect");
        });

        await ReallyBeIdle(pair, 10);

        if (!string.IsNullOrWhiteSpace(pair.Settings.ExtraPrototypes))
        {
            await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Removing prototypes");
            if (!pair.Settings.NoServer)
            {
                var serverProtoManager = pair.Server.ResolveDependency<IPrototypeManager>();
                await pair.Server.WaitPost(() =>
                {
                    serverProtoManager.RemoveString(pair.Settings.ExtraPrototypes.Trim());
                });
            }
            if (!pair.Settings.NoClient)
            {
                var clientProtoManager = pair.Client.ResolveDependency<IPrototypeManager>();
                await pair.Client.WaitPost(() =>
                {
                    clientProtoManager.RemoveString(pair.Settings.ExtraPrototypes.Trim());
                });
            }

            await ReallyBeIdle(pair, 1);
        }

        if (poolSettings.ExtraPrototypes != null)
        {
            await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Adding prototypes");
            if (!poolSettings.NoServer)
            {
                await ConfigurePrototypes(pair.Server, poolSettings);
            }
            if (!poolSettings.NoClient)
            {
                await ConfigurePrototypes(pair.Client, poolSettings);
            }
        }

        configManager.SetCVar(CCVars.GameMap, poolSettings.Map);
        await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Restarting server again");

        configManager.SetCVar(CCVars.GameMap, poolSettings.Map);
        configManager.SetCVar(CCVars.GameDummyTicker, poolSettings.DummyTicker);
        await pair.Server.WaitPost(() => gameTicker.RestartRound());

        if (!poolSettings.NotConnected)
        {
            await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Connecting client");
            await ReallyBeIdle(pair);
            pair.Client.SetConnectTarget(pair.Server);
            var netMgr = pair.Client.ResolveDependency<IClientNetManager>();
            await pair.Client.WaitPost(() =>
            {
                if (!netMgr.IsConnected)
                {
                    netMgr.ClientConnect(null!, 0, null!);
                }
            });
        }
        await ReallyBeIdle(pair);
        await testOut.WriteLineAsync($"Recycling: {methodWatch.Elapsed.TotalMilliseconds} ms: Done recycling");
    }

    private static void DieIfPoolFailure()
    {
        if (_poolFailureReason != null)
        {
            // If the _poolFailureReason is not null, we can assume at least one test failed.
            // So we say inconclusive so we don't add more failed tests to search through.
            Assert.Inconclusive(@"
In a different test, the pool manager had an exception when trying to create a server/client pair.
Instead of risking that the pool manager will fail at creating a server/client pairs for every single test,
we are just going to end this here to save a lot of time. This is the exception that started this:\n {0}", _poolFailureReason);
        }

        if (_dead)
        {
            // If Pairs is null, we ran out of time, we can't assume a test failed.
            // So we are going to tell it all future tests are a failure.
            Assert.Fail("The pool was shut down");
        }
    }
    private static async Task<Pair> CreateServerClientPair(PoolSettings poolSettings, TextWriter testOut)
    {
        Pair pair;
        try
        {
            var (client, clientLog) = await GenerateClient(poolSettings, testOut);
            var (server, serverLog) = await GenerateServer(poolSettings, testOut);
            pair = new Pair
            {
                Server = server,
                ServerLogHandler = serverLog,
                Client = client,
                ClientLogHandler = clientLog,
                PairId = Interlocked.Increment(ref _pairId)
            };
        }
        catch (Exception ex)
        {
            _poolFailureReason = ex;
            throw;
        }

        if (!poolSettings.NotConnected)
        {
            pair.Client.SetConnectTarget(pair.Server);
            await pair.Client.WaitPost(() =>
            {
                var netMgr = IoCManager.Resolve<IClientNetManager>();
                if (!netMgr.IsConnected)
                {
                    netMgr.ClientConnect(null!, 0, null!);
                }
            });
            await ReallyBeIdle(pair, 10);
            await pair.Client.WaitRunTicks(1);
        }
        return pair;
    }

    /// <summary>
    /// Creates a map, a grid, and a tile, and gives back references to them.
    /// </summary>
    /// <param name="pairTracker">A pairTracker</param>
    /// <returns>A TestMapData</returns>
    public static async Task<TestMapData> CreateTestMap(PairTracker pairTracker)
    {
        var server = pairTracker.Pair.Server;

        await server.WaitIdleAsync();

        var settings = pairTracker.Pair.Settings;
        var mapManager = server.ResolveDependency<IMapManager>();
        var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var xformSystem = entityManager.System<SharedTransformSystem>();

        if (settings.NoServer) throw new Exception("Cannot setup test map without server");
        var mapData = new TestMapData();
        await server.WaitPost(() =>
        {
            mapData.MapId = mapManager.CreateMap();
            mapData.MapUid = mapManager.GetMapEntityId(mapData.MapId);
            mapData.MapGrid = mapManager.CreateGrid(mapData.MapId);
            mapData.GridUid = mapData.MapGrid.Owner; // Fixing this requires an engine PR.
            mapData.GridCoords = new EntityCoordinates(mapData.GridUid, 0, 0);
            var plating = tileDefinitionManager["Plating"];
            var platingTile = new Tile(plating.TileId);
            mapData.MapGrid.SetTile(mapData.GridCoords, platingTile);
            mapData.MapCoords = new MapCoordinates(0, 0, mapData.MapId);
            mapData.Tile = mapData.MapGrid.GetAllTiles().First();
        });
        if (!settings.Disconnected)
        {
            await RunTicksSync(pairTracker.Pair, 10);
        }

        return mapData;
    }

    /// <summary>
    /// Runs a server/client pair in sync
    /// </summary>
    /// <param name="pair">A server/client pair</param>
    /// <param name="ticks">How many ticks to run them for</param>
    public static async Task RunTicksSync(Pair pair, int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            await pair.Server.WaitRunTicks(1);
            await pair.Client.WaitRunTicks(1);
        }
    }

    /// <summary>
    /// Runs the server/client in sync, but also ensures they are both idle each tick.
    /// </summary>
    /// <param name="pair">The server/client pair</param>
    /// <param name="runTicks">How many ticks to run</param>
    public static async Task ReallyBeIdle(Pair pair, int runTicks = 25)
    {
        for (var i = 0; i < runTicks; i++)
        {
            await pair.Client.WaitRunTicks(1);
            await pair.Server.WaitRunTicks(1);
            for (var idleCycles = 0; idleCycles < 4; idleCycles++)
            {
                await pair.Client.WaitIdleAsync();
                await pair.Server.WaitIdleAsync();
            }
        }
    }

    /// <summary>
    /// Run the server/clients until the ticks are synchronized.
    /// By default the client will be one tick ahead of the server.
    /// </summary>
    public static async Task SyncTicks(Pair pair, int targetDelta = 1)
    {
        var sTiming = pair.Server.ResolveDependency<IGameTiming>();
        var cTiming = pair.Client.ResolveDependency<IGameTiming>();
        var sTick = (int)sTiming.CurTick.Value;
        var cTick = (int)cTiming.CurTick.Value;
        var delta = cTick - sTick;

        if (delta == targetDelta)
            return;
        if (delta > targetDelta)
            await pair.Server.WaitRunTicks(delta - targetDelta);
        else
            await pair.Client.WaitRunTicks(targetDelta - delta);

        sTick = (int)sTiming.CurTick.Value;
        cTick = (int)cTiming.CurTick.Value;
        delta = cTick - sTick;
        Assert.That(delta, Is.EqualTo(targetDelta));
    }

    /// <summary>
    /// Runs a server, or a client until a condition is true
    /// </summary>
    /// <param name="instance">The server or client</param>
    /// <param name="func">The condition to check</param>
    /// <param name="maxTicks">How many ticks to try before giving up</param>
    /// <param name="tickStep">How many ticks to wait between checks</param>
    public static async Task WaitUntil(RobustIntegrationTest.IntegrationInstance instance, Func<bool> func,
        int maxTicks = 600,
        int tickStep = 1)
    {
        await WaitUntil(instance, async () => await Task.FromResult(func()), maxTicks, tickStep);
    }

    /// <summary>
    /// Runs a server, or a client until a condition is true
    /// </summary>
    /// <param name="instance">The server or client</param>
    /// <param name="func">The async condition to check</param>
    /// <param name="maxTicks">How many ticks to try before giving up</param>
    /// <param name="tickStep">How many ticks to wait between checks</param>
    public static async Task WaitUntil(RobustIntegrationTest.IntegrationInstance instance, Func<Task<bool>> func,
        int maxTicks = 600,
        int tickStep = 1)
    {
        var ticksAwaited = 0;
        bool passed;

        await instance.WaitIdleAsync();

        while (!(passed = await func()) && ticksAwaited < maxTicks)
        {
            var ticksToRun = tickStep;

            if (ticksAwaited + tickStep > maxTicks)
            {
                ticksToRun = maxTicks - ticksAwaited;
            }

            await instance.WaitRunTicks(ticksToRun);

            ticksAwaited += ticksToRun;
        }

        if (!passed)
        {
            Assert.Fail($"Condition did not pass after {maxTicks} ticks.\n" +
                        $"Tests ran ({instance.TestsRan.Count}):\n" +
                        $"{string.Join('\n', instance.TestsRan)}");
        }

        Assert.That(passed);
    }

    /// <summary>
    ///     Helper method that retrieves all entity prototypes that have some component.
    /// </summary>
    public static List<EntityPrototype> GetEntityPrototypes<T>(RobustIntegrationTest.IntegrationInstance instance) where T : Component
    {
        var protoMan = instance.ResolveDependency<IPrototypeManager>();
        var compFact = instance.ResolveDependency<IComponentFactory>();

        var id = compFact.GetComponentName(typeof(T));
        var list = new List<EntityPrototype>();
        foreach (var ent in protoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (ent.Components.ContainsKey(id))
                list.Add(ent);
        }

        return list;
    }
}

/// <summary>
/// Settings for the pooled server, and client pair.
/// Some options are for changing the pair, and others are
/// so the pool can properly clean up what you borrowed.
/// </summary>
public sealed class PoolSettings
{
    // TODO: We can make more of these pool-able, if we need enough of them for it to matter

    /// <summary>
    /// If the returned pair must not be reused
    /// </summary>
    public bool MustNotBeReused => Destructive || NoLoadContent || NoToolsExtraPrototypes;

    /// <summary>
    /// If the given pair must be brand new
    /// </summary>
    public bool MustBeNew => Fresh || NoLoadContent || NoToolsExtraPrototypes;

    /// <summary>
    /// If the given pair must not be connected
    /// </summary>
    public bool NotConnected => NoClient || NoServer || Disconnected;

    /// <summary>
    /// Set to true if the test will ruin the server/client pair.
    /// </summary>
    public bool Destructive { get; init; }

    /// <summary>
    /// Set to true if the given server/client pair should be created fresh.
    /// </summary>
    public bool Fresh { get; init; }

    /// <summary>
    /// Set to true if the given server should be using a dummy ticker.
    /// </summary>
    public bool DummyTicker { get; init; }

    /// <summary>
    /// Set to true if the given server/client pair should be disconnected from each other.
    /// </summary>
    public bool Disconnected { get; init; }

    /// <summary>
    /// Set to true if the given server/client pair should be in the lobby.
    /// If the pair is not in the lobby at the end of the test, this test must be marked as dirty.
    /// </summary>
    public bool InLobby { get; init; }

    /// <summary>
    /// Set this to true to skip loading the content files.
    /// Note: This setting won't work with a client.
    /// </summary>
    public bool NoLoadContent { get; init; }

    /// <summary>
    /// Set this to raw yaml text to load prototypes onto the given server/client pair.
    /// </summary>
    public string ExtraPrototypes { get; init; }

    /// <summary>
    /// Set this to true to disable the NetInterp CVar on the given server/client pair
    /// </summary>
    public bool DisableInterpolate { get; init; }

    /// <summary>
    /// Set this to true to always clean up the server/client pair before giving it to another borrower
    /// </summary>
    public bool Dirty { get; init; }

    /// <summary>
    /// Set this to the path of a map to have the given server/client pair load the map.
    /// </summary>
    public string Map { get; init; } = PoolManager.TestMap;

    /// <summary>
    /// Set to true if the test won't use the client (so we can skip cleaning it up)
    /// </summary>
    public bool NoClient { get; init; }

    /// <summary>
    /// Set to true if the test won't use the server (so we can skip cleaning it up)
    /// </summary>
    public bool NoServer { get; init; }

    /// <summary>
    /// Overrides the test name detection, and uses this in the test history instead
    /// </summary>
    public string TestName { get; set; }

    /// <summary>
    /// Tries to guess if we can skip recycling the server/client pair.
    /// </summary>
    /// <param name="nextSettings">The next set of settings the old pair will be set to</param>
    /// <returns>If we can skip cleaning it up</returns>
    public bool CanFastRecycle(PoolSettings nextSettings)
    {
        if (MustNotBeReused)
            throw new InvalidOperationException("Attempting to recycle a non-reusable test.");

        if (nextSettings.MustBeNew)
            throw new InvalidOperationException("Attempting to recycle a test while requesting a fresh test.");

        if (Dirty)
            return false;

        // Check that certain settings match.
        return NotConnected == nextSettings.NotConnected
               && DummyTicker == nextSettings.DummyTicker
               && Map == nextSettings.Map
               && InLobby == nextSettings.InLobby
               && ExtraPrototypes == nextSettings.ExtraPrototypes;
    }

    // Prototype hot reload is not available outside TOOLS builds,
    // so we can't pool test instances that use ExtraPrototypes without TOOLS.
#if TOOLS
#pragma warning disable CA1822 // Can't be marked as static b/c the other branch exists but Omnisharp can't see both.
    private bool NoToolsExtraPrototypes => false;
#pragma warning restore CA1822
#else
    private bool NoToolsExtraPrototypes => !string.IsNullOrEmpty(ExtraPrototypes);
#endif
}

/// <summary>
/// Holds a reference to things commonly needed when testing on a map
/// </summary>
public sealed class TestMapData
{
    public EntityUid MapUid { get; set; }
    public EntityUid GridUid { get; set; }
    public MapId MapId { get; set; }
    public MapGridComponent MapGrid { get; set; }
    public EntityCoordinates GridCoords { get; set; }
    public MapCoordinates MapCoords { get; set; }
    public TileRef Tile { get; set; }
}

/// <summary>
/// A server/client pair
/// </summary>
public sealed class Pair
{
    public bool Dead { get; private set; }
    public int PairId { get; init; }
    public List<string> TestHistory { get; set; } = new();
    public PoolSettings Settings { get; set; }
    public RobustIntegrationTest.ServerIntegrationInstance Server { get; init; }
    public RobustIntegrationTest.ClientIntegrationInstance Client { get; init; }

    public PoolTestLogHandler ServerLogHandler { get; init; }
    public PoolTestLogHandler ClientLogHandler { get; init; }

    public void Kill()
    {
        Dead = true;
        Server.Dispose();
        Client.Dispose();
    }

    public void ClearContext()
    {
        ServerLogHandler.ClearContext();
        ClientLogHandler.ClearContext();
    }

    public void ActivateContext(TextWriter testOut)
    {
        ServerLogHandler.ActivateContext(testOut);
        ClientLogHandler.ActivateContext(testOut);
    }
}

/// <summary>
/// Used by the pool to keep track of a borrowed server/client pair.
/// </summary>
public sealed class PairTracker : IAsyncDisposable
{
    private readonly TextWriter _testOut;
    private int _disposed;
    public Stopwatch UsageWatch { get; set; }
    public Pair Pair { get; init; }

    public PairTracker(TextWriter testOut)
    {
        _testOut = testOut;
    }

    private async Task OnDirtyDispose()
    {
        var usageTime = UsageWatch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Test gave back pair {Pair.PairId} in {usageTime.TotalMilliseconds} ms");
        var dirtyWatch = new Stopwatch();
        dirtyWatch.Start();
        Pair.Kill();
        PoolManager.NoCheckReturn(Pair);
        var disposeTime = dirtyWatch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Disposed pair {Pair.PairId} in {disposeTime.TotalMilliseconds} ms");
    }

    private async Task OnCleanDispose()
    {
        var usageTime = UsageWatch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Test borrowed pair {Pair.PairId} for {usageTime.TotalMilliseconds} ms");
        var cleanWatch = new Stopwatch();
        cleanWatch.Start();
        // Let any last minute failures the test cause happen.
        await PoolManager.ReallyBeIdle(Pair);
        if (!Pair.Settings.Destructive)
        {
            if (Pair.Client.IsAlive == false)
            {
                throw new Exception($"{nameof(CleanReturnAsync)}: Test killed the client in pair {Pair.PairId}:", Pair.Client.UnhandledException);
            }

            if (Pair.Server.IsAlive == false)
            {
                throw new Exception($"{nameof(CleanReturnAsync)}: Test killed the server in pair {Pair.PairId}:", Pair.Server.UnhandledException);
            }
        }

        if (Pair.Settings.MustNotBeReused)
        {
            Pair.Kill();
            PoolManager.NoCheckReturn(Pair);
            await PoolManager.ReallyBeIdle(Pair);
            var returnTime2 = cleanWatch.Elapsed;
            await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Clean disposed in {returnTime2.TotalMilliseconds} ms");
            return;
        }

        var sRuntimeLog = Pair.Server.ResolveDependency<IRuntimeLog>();
        if (sRuntimeLog.ExceptionCount > 0)
            throw new Exception($"{nameof(CleanReturnAsync)}: Server logged exceptions");
        var cRuntimeLog = Pair.Client.ResolveDependency<IRuntimeLog>();
        if (cRuntimeLog.ExceptionCount > 0)
            throw new Exception($"{nameof(CleanReturnAsync)}: Client logged exceptions");

        Pair.ClearContext();
        PoolManager.NoCheckReturn(Pair);
        var returnTime = cleanWatch.Elapsed;
        await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: PoolManager took {returnTime.TotalMilliseconds} ms to put pair {Pair.PairId} back into the pool");
    }

    public async ValueTask CleanReturnAsync()
    {
        var disposed = Interlocked.Exchange(ref _disposed, 1);
        switch (disposed)
        {
            case 0:
                await _testOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Return of pair {Pair.PairId} started");
                break;
            case 1:
                throw new Exception($"{nameof(CleanReturnAsync)}: Already clean returned");
            case 2:
                throw new Exception($"{nameof(CleanReturnAsync)}: Already dirty disposed");
            default:
                throw new Exception($"{nameof(CleanReturnAsync)}: Unexpected disposed value");
        }

        await OnCleanDispose();
    }

    public async ValueTask DisposeAsync()
    {
        var disposed = Interlocked.Exchange(ref _disposed, 2);
        switch (disposed)
        {
            case 0:
                await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Dirty return of pair {Pair.PairId} started");
                break;
            case 1:
                await _testOut.WriteLineAsync($"{nameof(DisposeAsync)}: Pair {Pair.PairId} was properly clean disposed");
                return;
            case 2:
                throw new Exception($"{nameof(DisposeAsync)}: Already dirty disposed pair {Pair.PairId}");
            default:
                throw new Exception($"{nameof(DisposeAsync)}: Unexpected disposed value");
        }
        await OnDirtyDispose();
    }
}
