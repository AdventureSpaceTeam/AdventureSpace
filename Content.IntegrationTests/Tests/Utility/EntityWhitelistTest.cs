﻿using System.IO;
using System.Threading.Tasks;
using Content.Server.Cabinet;
using Content.Server.GameObjects.Components;
using Content.Shared.Whitelist;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.IntegrationTests.Tests.Utility
{
    [TestFixture]
    [TestOf(typeof(EntityWhitelist))]
    public class EntityWhitelistTest : ContentIntegrationTest
    {
        private const string InvalidComponent = "Sprite";
        private const string ValidComponent = "Physics";

        private static readonly string Prototypes = $@"
- type: Tag
  id: ValidTag
- type: Tag
  id: InvalidTag

- type: entity
  id: WhitelistDummy
  components:
  - type: ItemCabinet
    whitelist:
      prototypes:
      - ValidPrototypeDummy
      components:
      - {ValidComponent}
      tags:
      - ValidTag

- type: entity
  id: InvalidComponentDummy
  components:
  - type: {InvalidComponent}
- type: entity
  id: InvalidTagDummy
  components:
  - type: Tag
    tags:
    - InvalidTag

- type: entity
  id: ValidComponentDummy
  components:
  - type: {ValidComponent}
- type: entity
  id: ValidTagDummy
  components:
  - type: Tag
    tags:
    - ValidTag";

        [Test]
        public async Task Test()
        {
            var serverOptions = new ServerContentIntegrationOption {ExtraPrototypes = Prototypes};
            var server = StartServer(serverOptions);

            await server.WaitIdleAsync();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                var mapId = new MapId(1);
                var mapCoordinates = new MapCoordinates(0, 0, mapId);

                var validComponent = entityManager.SpawnEntity("ValidComponentDummy", mapCoordinates);
                var validTag = entityManager.SpawnEntity("ValidTagDummy", mapCoordinates);

                var invalidComponent = entityManager.SpawnEntity("InvalidComponentDummy", mapCoordinates);
                var invalidTag = entityManager.SpawnEntity("InvalidTagDummy", mapCoordinates);

                // Test instantiated on its own
                var whitelistInst = new EntityWhitelist
                {
                    Components = new[] {$"{ValidComponent}"},
                    Tags = new[] {"ValidTag"}
                };
                whitelistInst.UpdateRegistrations();
                Assert.That(whitelistInst, Is.Not.Null);

                Assert.That(whitelistInst.Components, Is.Not.Null);
                Assert.That(whitelistInst.Tags, Is.Not.Null);

                Assert.That(whitelistInst.IsValid(validComponent), Is.True);
                Assert.That(whitelistInst.IsValid(validTag), Is.True);

                Assert.That(whitelistInst.IsValid(invalidComponent), Is.False);
                Assert.That(whitelistInst.IsValid(invalidTag), Is.False);

                // Test from serialized
                var dummy = entityManager.SpawnEntity("WhitelistDummy", mapCoordinates);
                var whitelistSer = dummy.GetComponent<ItemCabinetComponent>().Whitelist;
                Assert.That(whitelistSer, Is.Not.Null);

                Assert.That(whitelistSer.Components, Is.Not.Null);
                Assert.That(whitelistSer.Tags, Is.Not.Null);

                Assert.That(whitelistSer.IsValid(validComponent), Is.True);
                Assert.That(whitelistSer.IsValid(validTag), Is.True);

                Assert.That(whitelistSer.IsValid(invalidComponent), Is.False);
                Assert.That(whitelistSer.IsValid(invalidTag), Is.False);
            });
        }
    }
}
