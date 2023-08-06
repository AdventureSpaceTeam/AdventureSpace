using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using System.Linq;
using System.Numerics;

namespace Content.IntegrationTests.Tests.Body
{
    [TestFixture]
    [TestOf(typeof(LungSystem))]
    public sealed class LungTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: HumanLungDummy
  id: HumanLungDummy
  components:
  - type: SolutionContainerManager
  - type: Body
    prototype: Human
  - type: MobState
    allowedStates:
      - Alive
  - type: Damageable
  - type: ThermalRegulator
    metabolismHeat: 5000
    radiatedHeat: 400
    implicitHeatRegulation: 5000
    sweatHeatRegulation: 5000
    shiveringHeatRegulation: 5000
    normalBodyTemperature: 310.15
    thermalRegulationTemperatureThreshold: 25
  - type: Respirator
    damage:
      types:
        Asphyxiation: 1.5
    damageRecovery:
      types:
        Asphyxiation: -1.5
";

        [Test]
        public async Task AirConsistencyTest()
        {
            // --- Setup
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entityManager.System<MapLoaderSystem>();
            RespiratorSystem respSys = default;
            MetabolizerSystem metaSys = default;

            MapId mapId;
            EntityUid? grid = null;
            BodyComponent body = default;
            EntityUid human = default;
            GridAtmosphereComponent relevantAtmos = default;
            var startingMoles = 0.0f;

            var testMapName = "Maps/Test/Breathing/3by3-20oxy-80nit.yml";

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                Assert.That(mapLoader.TryLoad(mapId, testMapName, out var roots));

                var query = entityManager.GetEntityQuery<MapGridComponent>();
                var grids = roots.Where(x => query.HasComponent(x));
                Assert.That(grids, Is.Not.Empty);
                grid = grids.First();
            });

            Assert.That(grid, Is.Not.Null, $"Test blueprint {testMapName} not found.");

            float GetMapMoles()
            {
                var totalMapMoles = 0.0f;
                foreach (var tile in relevantAtmos.Tiles.Values)
                {
                    totalMapMoles += tile.Air?.TotalMoles ?? 0.0f;
                }

                return totalMapMoles;
            }

            await server.WaitAssertion(() =>
            {
                var coords = new Vector2(0.5f, -1f);
                var coordinates = new EntityCoordinates(grid.Value, coords);
                human = entityManager.SpawnEntity("HumanLungDummy", coordinates);
                respSys = entityManager.System<RespiratorSystem>();
                metaSys = entityManager.System<MetabolizerSystem>();
                relevantAtmos = entityManager.GetComponent<GridAtmosphereComponent>(grid.Value);
                startingMoles = GetMapMoles();

#pragma warning disable NUnit2045
                Assert.That(entityManager.TryGetComponent(human, out body), Is.True);
                Assert.That(entityManager.HasComponent<RespiratorComponent>(human), Is.True);
#pragma warning restore NUnit2045
            });

            // --- End setup

            var inhaleCycles = 100;
            for (var i = 0; i < inhaleCycles; i++)
            {
                await server.WaitAssertion(() =>
                {
                    // inhale
                    respSys.Update(2.0f);
                    Assert.That(GetMapMoles(), Is.LessThan(startingMoles));

                    // metabolize + exhale
                    metaSys.Update(1.0f);
                    metaSys.Update(1.0f);
                    respSys.Update(2.0f);
                    Assert.That(GetMapMoles(), Is.EqualTo(startingMoles).Within(0.0001));
                });
            }

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task NoSuffocationTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            var mapLoader = entityManager.System<MapLoaderSystem>();

            MapId mapId;
            EntityUid? grid = null;
            RespiratorComponent respirator = null;
            EntityUid human = default;

            var testMapName = "Maps/Test/Breathing/3by3-20oxy-80nit.yml";

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();

                Assert.That(mapLoader.TryLoad(mapId, testMapName, out var ents), Is.True);
                var query = entityManager.GetEntityQuery<MapGridComponent>();
                grid = ents
                    .Select<EntityUid, EntityUid?>(x => x)
                    .FirstOrDefault((uid) => uid.HasValue && query.HasComponent(uid.Value), null);
                Assert.That(grid, Is.Not.Null);
            });

            Assert.That(grid, Is.Not.Null, $"Test blueprint {testMapName} not found.");

            await server.WaitAssertion(() =>
            {
                var center = new Vector2(0.5f, 0.5f);

                var coordinates = new EntityCoordinates(grid.Value, center);
                human = entityManager.SpawnEntity("HumanLungDummy", coordinates);

                var mixture = entityManager.System<AtmosphereSystem>().GetContainingMixture(human);
#pragma warning disable NUnit2045
                Assert.That(mixture.TotalMoles, Is.GreaterThan(0));
                Assert.That(entityManager.HasComponent<BodyComponent>(human), Is.True);
                Assert.That(entityManager.TryGetComponent(human, out respirator), Is.True);
                Assert.That(respirator.SuffocationCycles, Is.LessThanOrEqualTo(respirator.SuffocationCycleThreshold));
#pragma warning restore NUnit2045
            });

            var increment = 10;

            // 20 seconds
            var total = 20 * cfg.GetCVar(CVars.NetTickrate);

            for (var tick = 0; tick < total; tick += increment)
            {
                await server.WaitRunTicks(increment);
                await server.WaitAssertion(() =>
                {
                    Assert.That(respirator.SuffocationCycles, Is.LessThanOrEqualTo(respirator.SuffocationCycleThreshold),
                        $"Entity {entityManager.GetComponent<MetaDataComponent>(human).EntityName} is suffocating on tick {tick}");
                });
            }

            await pairTracker.CleanReturnAsync();
        }
    }
}
