#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Cuffs.Components;
using Content.Server.Hands.Components;
using Content.Shared.Body.Components;
using NUnit.Framework;
using Robust.Server.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using HandsComponent = Content.Client.Hands.HandsComponent;

namespace Content.IntegrationTests.Tests.GameObjects.Components.ActionBlocking
{
    [TestFixture]
    [TestOf(typeof(CuffableComponent))]
    [TestOf(typeof(HandcuffComponent))]
    public class HandCuffTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Cuffable
  - type: Hands
  - type: Body
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso

- type: entity
  name: HandcuffsDummy
  id: HandcuffsDummy
  components:
  - type: Handcuff
";
        [Test]
        public async Task Test()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            IEntity human;
            IEntity otherHuman;
            IEntity cuffs;
            IEntity secondCuffs;
            CuffableComponent cuffed;
            IHandsComponent hands;

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                human = entityManager.SpawnEntity("HumanDummy", coordinates);
                otherHuman = entityManager.SpawnEntity("HumanDummy", coordinates);
                cuffs = entityManager.SpawnEntity("HandcuffsDummy", coordinates);
                secondCuffs = entityManager.SpawnEntity("HandcuffsDummy", coordinates);

                human.Transform.WorldPosition = otherHuman.Transform.WorldPosition;

                // Test for components existing
                Assert.True(human.TryGetComponent(out cuffed!), $"Human has no {nameof(CuffableComponent)}");
                Assert.True(human.TryGetComponent(out hands!), $"Human has no {nameof(HandsComponent)}");
                Assert.True(human.TryGetComponent(out SharedBodyComponent _), $"Human has no {nameof(SharedBodyComponent)}");
                Assert.True(cuffs.TryGetComponent(out HandcuffComponent _), $"Handcuff has no {nameof(HandcuffComponent)}");
                Assert.True(secondCuffs.TryGetComponent(out HandcuffComponent _), $"Second handcuffs has no {nameof(HandcuffComponent)}");

                // Test to ensure cuffed players register the handcuffs
                cuffed.TryAddNewCuffs(human, cuffs);
                Assert.True(cuffed.CuffedHandCount > 0, "Handcuffing a player did not result in their hands being cuffed");

                // Test to ensure a player with 4 hands will still only have 2 hands cuffed
                AddHand(cuffed.Owner);
                AddHand(cuffed.Owner);
                Assert.That(cuffed.CuffedHandCount, Is.EqualTo(2));
                Assert.That(hands.Hands.Count(), Is.EqualTo(4));

                // Test to give a player with 4 hands 2 sets of cuffs
                cuffed.TryAddNewCuffs(human, secondCuffs);
                Assert.True(cuffed.CuffedHandCount == 4, "Player doesn't have correct amount of hands cuffed");

            });

            await server.WaitIdleAsync();
        }

        private void AddHand(IEntity to)
        {
            var host = IoCManager.Resolve<IServerConsoleHost>();
            host.ExecuteCommand(null, $"addhand {to.Uid}");
        }
    }
}
