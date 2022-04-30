﻿#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.Painters;
using SixLabors.ImageSharp;

namespace Content.MapRenderer
{
    internal class Program
    {
        private const string MapsAddedEnvKey = "FILES_ADDED";
        private const string MapsModifiedEnvKey = "FILES_MODIFIED";

        private static readonly MapPainter MapPainter = new();

        internal static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Didn't specify any maps to paint! Provide map names (as map prototype names).");
            }

            // var created = Environment.GetEnvironmentVariable(MapsAddedEnvKey);
            // var modified = Environment.GetEnvironmentVariable(MapsModifiedEnvKey);
            //
            // var yamlStream = new YamlStream();
            //
            // if (created != null)
            // {
            //     yamlStream.Load(new StringReader(created));
            // }
            //
            // if (modified != null)
            // {
            //     yamlStream.Load(new StringReader(modified));
            // }
            //
            // var files = new YamlSequenceNode();
            //
            // foreach (var doc in yamlStream.Documents)
            // {
            //     var filesModified = (YamlSequenceNode) doc.RootNode;
            //
            //     foreach (var node in filesModified)
            //     {
            //         files.Add(node);
            //     }
            // }

            // var maps = new List<string>();

            // foreach (var node in files)
            // {
            //     var fileName = node.AsString();
            //
            //     if (!fileName.StartsWith("Resources/Maps/") ||
            //         !fileName.EndsWith("yml"))
            //     {
            //         continue;
            //     }
            //
            //     maps.Add(fileName);
            // }

            await Run(new List<string>(args));
        }

        private static async Task Run(List<string> maps)
        {
            Console.WriteLine($"Creating images for {maps.Count} maps");

            var mapNames = new List<string>();
            foreach (var map in maps)
            {
                Console.WriteLine($"Painting map {map}");

                int i = 0;
                await foreach (var grid in MapPainter.Paint(map))
                {
                    var directory = DirectoryExtensions.MapImages().FullName;
                    Directory.CreateDirectory(directory);

                    var fileName = Path.GetFileNameWithoutExtension(map);
                    var savePath = $"{directory}{Path.DirectorySeparatorChar}{fileName}-{i}.png";

                    Console.WriteLine($"Writing grid of size {grid.Width}x{grid.Height} to {savePath}");

                    await grid.SaveAsPngAsync(savePath);
                    grid.Dispose();

                    mapNames.Add(fileName);
                    i++;
                }
            }

            var mapNamesString = $"[{string.Join(',', mapNames.Select(s => $"\"{s}\""))}]";
            Console.WriteLine($@"::set-output name=map_names::{mapNamesString}");
            Console.WriteLine($"Created {maps.Count} map images.");
        }
    }
}
