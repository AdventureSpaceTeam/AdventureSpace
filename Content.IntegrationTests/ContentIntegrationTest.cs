using System;
using System.Threading.Tasks;
using Content.Client;
using Content.Client.Interfaces.Parallax;
using Content.Server;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.UnitTesting;
using EntryPoint = Content.Client.EntryPoint;

namespace Content.IntegrationTests
{
    public abstract class ContentIntegrationTest : RobustIntegrationTest
    {
        protected sealed override ClientIntegrationInstance StartClient(ClientIntegrationOptions options = null)
        {
            options ??= new ClientIntegrationOptions();

            // ReSharper disable once RedundantNameQualifier
            options.ClientContentAssembly = typeof(EntryPoint).Assembly;
            options.SharedContentAssembly = typeof(Shared.EntryPoint).Assembly;
            options.BeforeStart += () =>
            {
                IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
                {
                    ClientBeforeIoC = () =>
                    {
                        if (options is ClientContentIntegrationOption contentOptions)
                        {
                            contentOptions.ContentBeforeIoC?.Invoke();
                        }

                        IoCManager.Register<IParallaxManager, DummyParallaxManager>(true);
                    }
                });
            };

            // Connecting to Discord is a massive waste of time.
            // Basically just makes the CI logs a mess.
            options.CVarOverrides["discord.enabled"] = "true";

            return base.StartClient(options);
        }

        protected override ServerIntegrationInstance StartServer(ServerIntegrationOptions options = null)
        {
            options ??= new ServerIntegrationOptions();
            options.ServerContentAssembly = typeof(Server.EntryPoint).Assembly;
            options.SharedContentAssembly = typeof(Shared.EntryPoint).Assembly;
            return base.StartServer(options);
        }

        protected ServerIntegrationInstance StartServerDummyTicker(ServerIntegrationOptions options = null)
        {
            options ??= new ServerIntegrationOptions();
            options.BeforeStart += () =>
            {
                IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ServerModuleTestingCallbacks
                {
                    ServerBeforeIoC = () =>
                    {
                        if (options is ServerContentIntegrationOption contentOptions)
                        {
                            contentOptions.ContentBeforeIoC?.Invoke();
                        }

                        IoCManager.Register<IGameTicker, DummyGameTicker>(true);
                    }
                });
            };

            return StartServer(options);
        }

        protected async Task<(ClientIntegrationInstance client, ServerIntegrationInstance server)> StartConnectedServerClientPair(ClientIntegrationOptions clientOptions = null, ServerIntegrationOptions serverOptions = null)
        {
            var client = StartClient(clientOptions);
            var server = StartServerDummyTicker(serverOptions);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            client.SetConnectTarget(server);

            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            await RunTicksSync(client, server, 10);

            return (client, server);
        }

        /// <summary>
        ///     Runs <paramref name="ticks"/> ticks on both server and client while keeping their main loop in sync.
        /// </summary>
        protected static async Task RunTicksSync(ClientIntegrationInstance client, ServerIntegrationInstance server, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }

        protected sealed class ClientContentIntegrationOption : ClientIntegrationOptions
        {
            public Action ContentBeforeIoC { get; set; }
        }

        protected sealed class ServerContentIntegrationOption : ServerIntegrationOptions
        {
            public Action ContentBeforeIoC { get; set; }
        }
    }
}
