﻿using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Timing;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(Robust.Shared.GameObjects.Entity))]
    public class EntityTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var mapMan = server.ResolveDependency<IMapManager>();
            var entityMan = server.ResolveDependency<IEntityManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var pauseMan = server.ResolveDependency<IPauseManager>();
            var prototypes = new List<EntityPrototype>();
            IMapGrid grid = default;
            IEntity testEntity = null;

            //Build up test environment
            server.Post(() =>
            {
                var mapId = mapMan.CreateMap();
                pauseMan.AddUninitializedMap(mapId);
                grid = mapLoader.LoadBlueprint(mapId, "Maps/stationstation.yml");
            });

            server.Assert(() =>
                {
                    var testLocation = new GridCoordinates(new Robust.Shared.Maths.Vector2(0, 0), grid);

                    //Generate list of non-abstract prototypes to test
                    foreach (var prototype in prototypeMan.EnumeratePrototypes<EntityPrototype>())
                    {
                        if (prototype.Abstract)
                        {
                            continue;
                        }
                        prototypes.Add(prototype);
                    }

                    //Iterate list of prototypes to spawn
                    foreach (var prototype in prototypes)
                    {
                        try
                        {
                            Logger.LogS(LogLevel.Debug, "EntityTest", "Testing: " + prototype.ID);
                            testEntity = entityMan.SpawnEntity(prototype.ID, testLocation);
                            server.RunTicks(2);
                            Assert.That(testEntity.Initialized);
                            entityMan.DeleteEntity(testEntity.Uid);
                        }

                        //Fail any exceptions thrown on spawn
                        catch (Exception e)
                        {
                            Logger.LogS(LogLevel.Error, "EntityTest", "Entity '" + prototype.ID + "' threw: " + e.Message);
                            Assert.Fail();
                        }
                    }
                });

            await server.WaitIdleAsync();
        }

    }
}
