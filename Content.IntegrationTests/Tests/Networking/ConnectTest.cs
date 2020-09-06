using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.Networking
{
    [TestFixture]
    public class ConnectTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestConnect()
        {
            var client = StartClient();
            var server = StartServer();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Connect.

            client.SetConnectTarget(server);

            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.

            for (var i = 0; i < 10; i++)
            {
                server.RunTicks(1);
                await server.WaitIdleAsync();
                client.RunTicks(1);
                await client.WaitIdleAsync();
            }

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Basic checks to ensure that they're connected and data got replicated.

            var playerManager = server.ResolveDependency<IPlayerManager>();
            Assert.That(playerManager.PlayerCount, Is.EqualTo(1));
            Assert.That(playerManager.GetAllPlayers().First().Status, Is.EqualTo(SessionStatus.InGame));

            var clEntityManager = client.ResolveDependency<IEntityManager>();
            var svEntityManager = server.ResolveDependency<IEntityManager>();

            var lastSvEntity = svEntityManager.GetEntities().Last();
            var lastClEntity = clEntityManager.GetEntity(lastSvEntity.Uid);

            Assert.That(lastClEntity.Transform.Coordinates, Is.EqualTo(lastSvEntity.Transform.Coordinates));
        }
    }
}
