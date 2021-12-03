using System;
using System.Threading.Tasks;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
            var server = StartServer();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();

            server.Assert(() =>
            {
                var solution = new Solution("Water", FixedPoint2.New(20));
                var grid = GetMainGrid(mapManager);
                var (x, y) = GetMainTile(grid).GridIndices;
                var coordinates = new EntityCoordinates(grid.GridEntityId, x, y);
                var puddle = solution.SpillAt(coordinates, "PuddleSmear");

                Assert.NotNull(puddle);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task SpaceNoPuddleTest()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();

            IMapGrid grid = null;

            // Remove all tiles
            server.Post(() =>
            {
                grid = GetMainGrid(mapManager);

                foreach (var tile in grid.GetAllTiles())
                {
                    grid.SetTile(tile.GridIndices, Tile.Empty);
                }
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                var coordinates = grid.ToCoordinates();
                var solution = new Solution("Water", FixedPoint2.New(20));
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
                IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(sGridEntity.Uid).EntityPaused = true; // See https://github.com/space-wizards/RobustToolbox/issues/1444

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
                Assert.True((!IoCManager.Resolve<IEntityManager>().EntityExists(sGridEntity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(sGridEntity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted || IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(sGridEntity.Uid).EntityPaused);
            });

            float evaporateTime = default;
            PuddleComponent puddle = null;
            EvaporationComponent evaporation;
            var amount = 2;

            // Spawn a puddle
            await server.WaitAssertion(() =>
            {
                var solution = new Solution("Water", FixedPoint2.New(amount));
                puddle = solution.SpillAt(sCoordinates, "PuddleSmear");

                // Check that the puddle was created
                Assert.NotNull(puddle);

                evaporation = IoCManager.Resolve<IEntityManager>().GetComponent<EvaporationComponent>(puddle.Owner.Uid);

                IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityPaused = true; // See https://github.com/space-wizards/RobustToolbox/issues/1445

                Assert.True((!IoCManager.Resolve<IEntityManager>().EntityExists(puddle.Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted || IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityPaused);

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
                Assert.True((!IoCManager.Resolve<IEntityManager>().EntityExists(puddle.Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted || IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityPaused);

                // Check that the puddle still exists
                Assert.False((!IoCManager.Resolve<IEntityManager>().EntityExists(puddle.Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted);
            });

            // Unpause the map
            await server.WaitPost(() => { sPauseManager.SetMapPaused(sMapId, false); });

            // Check that the map, grid and puddle are unpaused
            await server.WaitAssertion(() =>
            {
                Assert.False(sPauseManager.IsMapPaused(sMapId));
                Assert.False(sPauseManager.IsGridPaused(sGridId));
                Assert.False((!IoCManager.Resolve<IEntityManager>().EntityExists(puddle.Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted || IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityPaused);

                // Check that the puddle still exists
                Assert.False((!IoCManager.Resolve<IEntityManager>().EntityExists(puddle.Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(puddle.Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted);
            });

            // Wait enough time for it to evaporate
            await server.WaitRunTicks(sTimeToWait);

            // Puddle evaporation should have ticked
            await server.WaitAssertion(() =>
            {
                // Check that puddle has been deleted
                Assert.True(puddle.Deleted);
            });
        }
    }
}
