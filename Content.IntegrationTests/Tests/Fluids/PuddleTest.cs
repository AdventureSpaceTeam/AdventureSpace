using System;
using System.Threading.Tasks;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Fluids
{
    [TestFixture]
    [TestOf(typeof(PuddleComponent))]
    public class PuddleTest : ContentIntegrationTest
    {
        [Test]
        public async Task TilePuddleTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            EntityCoordinates coordinates = default;

            // Build up test environment
            server.Post(() =>
            {
                // Create a one tile grid to spill onto
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out var grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                pauseManager.DoMapInitialize(mapId);
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                var solution = new Solution("water", FixedPoint2.New(20));
                var puddle = solution.SpillAt(coordinates, "PuddleSmear");
                Assert.NotNull(puddle);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task SpaceNoPuddleTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();
            var mapManager = server.ResolveDependency<IMapManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            IMapGrid grid = null;

            // Build up test environment
            server.Post(() =>
            {
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                var coordinates = grid.ToCoordinates();
                var solution = new Solution("water", FixedPoint2.New(20));
                var puddle = solution.SpillAt(coordinates, "PuddleSmear");
                Assert.Null(puddle);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task PuddlePauseTest()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sPauseManager = server.ResolveDependency<IPauseManager>();
            var sTileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
            var sGameTiming = server.ResolveDependency<IGameTiming>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();

            MapId sMapId = default;
            IMapGrid sGrid;
            GridId sGridId = default;
            IEntity sGridEntity = null;
            EntityCoordinates sCoordinates = default;

            // Spawn a paused map with one tile to spawn puddles on
            await server.WaitPost(() =>
            {
                sMapId = sMapManager.CreateMap();
                sPauseManager.SetMapPaused(sMapId, true);
                sGrid = sMapManager.CreateGrid(sMapId);
                sGridId = sGrid.Index;
                sGridEntity = sEntityManager.GetEntity(sGrid.GridEntityId);
                sGridEntity.Paused = true; // See https://github.com/space-wizards/RobustToolbox/issues/1444

                var tileDefinition = sTileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                sCoordinates = sGrid.ToCoordinates();

                sGrid.SetTile(sCoordinates, tile);
            });

            // Check that the map and grid are paused
            await server.WaitAssertion(() =>
            {
                Assert.True(sPauseManager.IsGridPaused(sGridId));
                Assert.True(sPauseManager.IsMapPaused(sMapId));
                Assert.True(sGridEntity.Paused);
            });

            float evaporateTime = default;
            PuddleComponent puddle = null;
            EvaporationComponent evaporation;
            var amount = 2;

            // Spawn a puddle
            await server.WaitAssertion(() =>
            {
                var solution = new Solution("water", FixedPoint2.New(amount));
                puddle = solution.SpillAt(sCoordinates, "PuddleSmear");

                // Check that the puddle was created
                Assert.NotNull(puddle);

                evaporation = puddle.Owner.GetComponent<EvaporationComponent>();

                puddle.Owner.Paused = true; // See https://github.com/space-wizards/RobustToolbox/issues/1445

                Assert.True(puddle.Owner.Paused);

                // Check that the puddle is going to evaporate
                Assert.Positive(evaporation.EvaporateTime);

                // Should have a timer component added to it for evaporation
                Assert.That(evaporation.Accumulator, Is.EqualTo(0f));

                evaporateTime = evaporation.EvaporateTime;
            });

            // Wait enough time for it to evaporate if it was unpaused
            var sTimeToWait = (5 + (int)Math.Ceiling(amount * evaporateTime * sGameTiming.TickRate));
            await server.WaitRunTicks(sTimeToWait);

            // No evaporation due to being paused
            await server.WaitAssertion(() =>
            {
                Assert.True(puddle.Owner.Paused);

                // Check that the puddle still exists
                Assert.False(puddle.Owner.Deleted);
            });

            // Unpause the map
            await server.WaitPost(() => { sPauseManager.SetMapPaused(sMapId, false); });

            // Check that the map, grid and puddle are unpaused
            await server.WaitAssertion(() =>
            {
                Assert.False(sPauseManager.IsMapPaused(sMapId));
                Assert.False(sPauseManager.IsGridPaused(sGridId));
                Assert.False(puddle.Owner.Paused);

                // Check that the puddle still exists
                Assert.False(puddle.Owner.Deleted);
            });

            // Wait enough time for it to evaporate
            await server.WaitRunTicks(sTimeToWait);

            // Puddle evaporation should have ticked
            await server.WaitAssertion(() =>
            {
                // Check that the puddle is unpaused
                Assert.False(puddle.Owner.Paused);

                // Check that puddle has been deleted
                Assert.True(puddle.Deleted);
            });
        }
    }
}
