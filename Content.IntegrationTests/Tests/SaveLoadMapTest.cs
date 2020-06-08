﻿using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Interfaces.Maps;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    class SaveLoadMapTest : ContentIntegrationTest
    {
        [Test]
        public async Task SaveLoadMultiGridMap()
        {
            const string mapPath = @"Maps/Test/TestMap.yml";

            var server = StartServer();
            await server.WaitIdleAsync();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            server.Post(() =>
            {
                var mapId = mapManager.CreateMap(new MapId(5));

                {
                    var mapGrid = mapManager.CreateGrid(mapId);
                    var mapGridEnt = entityManager.GetEntity(mapGrid.GridEntityId);
                    mapGridEnt.Transform.WorldPosition = new Vector2(10, 10);
                    mapGrid.SetTile(new MapIndices(0,0), new Tile(1, 512));
                }
                {
                    var mapGrid = mapManager.CreateGrid(mapId);
                    var mapGridEnt = entityManager.GetEntity(mapGrid.GridEntityId);
                    mapGridEnt.Transform.WorldPosition = new Vector2(-8, -8);
                    mapGrid.SetTile(new MapIndices(0, 0), new Tile(2, 511));
                }

                mapLoader.SaveMap(mapId, mapPath);

                mapManager.DeleteMap(new MapId(5));
            });
            await server.WaitIdleAsync();

            server.Post(() =>
            {
                mapLoader.LoadMap(new MapId(10), mapPath);
            });
            await server.WaitIdleAsync();

            {
                if(!mapManager.TryFindGridAt(new MapId(10), new Vector2(10,10), out var mapGrid))
                    Assert.Fail();

                Assert.AreEqual(new Vector2(10, 10), mapGrid.WorldPosition);
                Assert.AreEqual(new Tile(1, 512), mapGrid.GetTileRef(new MapIndices(0, 0)).Tile);
            }
            {
                if (!mapManager.TryFindGridAt(new MapId(10), new Vector2(-8, -8), out var mapGrid))
                    Assert.Fail();

                Assert.AreEqual(new Vector2(-8, -8), mapGrid.WorldPosition);
                Assert.AreEqual(new Tile(2, 511), mapGrid.GetTileRef(new MapIndices(0, 0)).Tile);
            }

        }
    }
}
