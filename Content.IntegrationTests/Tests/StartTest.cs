using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Exceptions;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class StartTest : ContentIntegrationTest
    {
        /// <summary>
        ///     Test that the server starts.
        /// </summary>
        [Test]
        public async Task TestServerStart()
        {
            var server = StartServer();
            server.RunTicks(5);
            await server.WaitIdleAsync();
            Assert.That(server.IsAlive);
            var runtimeLog = server.ResolveDependency<IRuntimeLog>();
            Assert.That(runtimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged.");
            server.Stop();
            await server.WaitIdleAsync();
            Assert.That(!server.IsAlive);
        }

        /// <summary>
        ///     Test that the client starts.
        /// </summary>
        [Test]
        public async Task TestClientStart()
        {
            var client = StartClient();
            await client.WaitIdleAsync();
            Assert.That(client.IsAlive);
            client.RunTicks(5);
            await client.WaitIdleAsync();
            Assert.That(client.IsAlive);
            var runtimeLog = client.ResolveDependency<IRuntimeLog>();
            Assert.That(runtimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged.");
            client.Stop();
            await client.WaitIdleAsync();
            Assert.That(!client.IsAlive);
        }
    }
}
