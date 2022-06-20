﻿#nullable enable
using System.Threading.Tasks;
using Content.Shared.Physics;
using Content.Shared.Spawning;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.IntegrationTests.Tests.Utility
{
    [TestFixture]
    [TestOf(typeof(EntitySystemExtensions))]
    public sealed class EntitySystemExtensionsTest
    {
        private const string BlockerDummyId = "BlockerDummy";

        private static readonly string Prototypes = $@"
- type: entity
  id: {BlockerDummyId}
  name: {BlockerDummyId}
  components:
  - type: Physics
  - type: Fixtures
    fixtures:
    - shape:
        !type:PhysShapeAabb
          bounds: ""-0.49,-0.49,0.49,0.49""
      mask:
      - Impassable
";

        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var broady = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedBroadphaseSystem>();

            await server.WaitAssertion(() =>
            {
                var grid = PoolManager.GetMainGrid(sMapManager);
                var gridEnt = grid.GridEntityId;
                var gridPos = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt).WorldPosition;
                var entityCoordinates = PoolManager.GetMainEntityCoordinates(sMapManager);

                // Nothing blocking it, only entity is the grid
                Assert.NotNull(sEntityManager.SpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.Impassable));
                Assert.True(sEntityManager.TrySpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.Impassable, out var entity));
                Assert.NotNull(entity);

                var mapId = PoolManager.GetMainGrid(sMapManager).ParentMapId;
                var mapCoordinates = new MapCoordinates(gridPos.X, gridPos.Y, mapId);

                // Nothing blocking it, only entity is the grid
                Assert.NotNull(sEntityManager.SpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.Impassable));
                Assert.True(sEntityManager.TrySpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.Impassable, out entity));
                Assert.NotNull(entity);

                // Spawn a blocker with an Impassable mask
                sEntityManager.SpawnEntity(BlockerDummyId, entityCoordinates);
                broady.Update(0.016f);

                // Cannot spawn something with an Impassable layer
                Assert.Null(sEntityManager.SpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.Impassable));
                Assert.False(sEntityManager.TrySpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.Impassable, out entity));
                Assert.Null(entity);

                Assert.Null(sEntityManager.SpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.Impassable));
                Assert.False(sEntityManager.TrySpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.Impassable, out entity));
                Assert.Null(entity);

                // Other layers are fine
                Assert.NotNull(sEntityManager.SpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.MidImpassable));
                Assert.True(sEntityManager.TrySpawnIfUnobstructed(null, entityCoordinates, CollisionGroup.MidImpassable, out entity));
                Assert.NotNull(entity);

                Assert.NotNull(sEntityManager.SpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.MidImpassable));
                Assert.True(sEntityManager.TrySpawnIfUnobstructed(null, mapCoordinates, CollisionGroup.MidImpassable, out entity));
                Assert.NotNull(entity);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
