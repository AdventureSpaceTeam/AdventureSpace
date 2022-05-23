﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Map;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class PostMapInitTest : ContentIntegrationTest
    {
        public const bool SkipTestMaps = true;
        public const string TestMapsPath = "/Maps/Test/";

        [Test]
        public async Task NoSavedPostMapInitTest()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var resourceManager = server.ResolveDependency<IResourceManager>();
            var mapFolder = new ResourcePath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                .ToArray();

            foreach (var map in maps)
            {
                var rootedPath = map.ToRootedPath();

                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath))
                {
                    continue;
                }

                if (!resourceManager.TryContentFileRead(rootedPath, out var fileStream))
                {
                    Assert.Fail($"Map not found: {rootedPath}");
                }

                using var reader = new StreamReader(fileStream);
                var yamlStream = new YamlStream();

                yamlStream.Load(reader);

                var root = yamlStream.Documents[0].RootNode;
                var meta = root["meta"];
                var postMapInit = meta["postmapinit"].AsBool();

                Assert.False(postMapInit, $"Map {map.Filename} was saved postmapinit");
            }
        }

        [Test]
        public async Task MapsLoadableTest()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var resourceManager = server.ResolveDependency<IResourceManager>();
            var mapFolder = new ResourcePath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                .ToArray();

            foreach (var map in maps)
            {
                var rootedPath = map.ToRootedPath();

                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath))
                {
                    continue;
                }

                server.Post(() =>
                {
                    var mapId = mapManager.CreateMap();
                    try
                    {
                        mapLoader.LoadMap(mapId, rootedPath.ToString());
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to load map {rootedPath}", ex);
                    }
                    try
                    {
                        mapManager.DeleteMap(mapId);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to delete map {rootedPath}", ex);
                    }
                });
                await server.WaitIdleAsync();
            }
        }
    }
}
