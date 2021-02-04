using System;
using System.Threading.Tasks;
using Content.Server.Commands.GameTicking;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared;
using NUnit.Framework;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(NewRoundCommand))]
    public class RestartRoundTest : ContentIntegrationTest
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task RestartRoundAfterStart(bool lobbyEnabled)
        {
            var (_, server) = await StartConnectedServerClientPair();

            await server.WaitIdleAsync();

            var gameTicker = server.ResolveDependency<IGameTicker>();
            var configManager = server.ResolveDependency<IConfigurationManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            await server.WaitRunTicks(30);

            GameTick tickBeforeRestart = default;

            server.Assert(() =>
            {
                configManager.SetCVar(CCVars.GameLobbyEnabled, lobbyEnabled);

                Assert.That(gameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

                tickBeforeRestart = entityManager.CurrentTick;

                var command = new NewRoundCommand();
                command.Execute(null, string.Empty, Array.Empty<string>());

                if (lobbyEnabled)
                {
                    Assert.That(gameTicker.RunLevel, Is.Not.EqualTo(GameRunLevel.InRound));
                }
            });

            await server.WaitIdleAsync();
            await server.WaitRunTicks(5);

            server.Assert(() =>
            {
                var tickAfterRestart = entityManager.CurrentTick;

                Assert.That(tickBeforeRestart < tickAfterRestart);
            });

            await server.WaitRunTicks(60);
        }
    }
}
