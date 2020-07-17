using System.Threading.Tasks;
using Content.Server.Interfaces.GameTicking;
using NUnit.Framework;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class RestartRoundTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var (client, server) = await StartConnectedServerClientPair();

            server.Post(() =>
            {
                IoCManager.Resolve<IGameTicker>().RestartRound();
            });

            await RunTicksSync(client, server, 10);
        }
    }
}
