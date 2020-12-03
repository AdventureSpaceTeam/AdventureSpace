﻿using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Mapping)]
    class TileWallsCommand : IClientCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "tilewalls";
        public string Description => "Puts an underplating tile below every wall on a grid.";
        public string Help => $"Usage: {Command} <gridId> | {Command}";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            GridId gridId;

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity == null)
                    {
                        shell.SendText((IPlayerSession) null, "Only a player can run this command.");
                        return;
                    }

                    gridId = player.AttachedEntity.Transform.GridID;
                    break;
                case 1:
                    if (!int.TryParse(args[0], out var id))
                    {
                        shell.SendText(player, $"{args[0]} is not a valid integer.");
                        return;
                    }

                    gridId = new GridId(id);
                    break;
                default:
                    shell.SendText(player, Help);
                    return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.SendText(player, $"No grid exists with id {gridId}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.TryGetEntity(grid.GridEntityId, out var gridEntity))
            {
                shell.SendText(player, $"Grid {gridId} doesn't have an associated grid entity.");
                return;
            }

            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var underplating = tileDefinitionManager["underplating"];
            var underplatingTile = new Tile(underplating.TileId);
            var changed = 0;
            foreach (var childUid in gridEntity.Transform.ChildEntityUids)
            {
                if (!entityManager.TryGetEntity(childUid, out var childEntity))
                {
                    continue;
                }

                var prototype = childEntity.Prototype;
                while (true)
                {
                    if (prototype?.Parent == null)
                    {
                        break;
                    }

                    prototype = prototype.Parent;
                }

                if (prototype?.ID != "base_wall")
                {
                    continue;
                }

                if (!childEntity.TryGetComponent(out SnapGridComponent snapGrid))
                {
                    continue;
                }

                var tile = grid.GetTileRef(childEntity.Transform.Coordinates);
                var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];

                if (tileDef.Name == "underplating")
                {
                    continue;
                }

                grid.SetTile(childEntity.Transform.Coordinates, underplatingTile);
                changed++;
            }

            shell.SendText(player, $"Changed {changed} tiles.");
        }
    }
}
