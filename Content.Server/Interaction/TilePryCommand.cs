using Content.Server.Administration;
using Content.Server.Tools.Components;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Interaction
{
    /// <summary>
    /// <see cref="TilePryingComponent.TryPryTile"/>
    /// </summary>
    [AdminCommand(AdminFlags.Debug)]
    class TilePryCommand : IConsoleCommand
    {
        public string Command => "tilepry";
        public string Description => "Pries up all tiles in a radius around the user.";
        public string Help => $"Usage: {Command} <radius>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
            {
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var radius))
            {
                shell.WriteLine($"{args[0]} isn't a valid integer.");
                return;
            }

            if (radius < 0)
            {
                shell.WriteLine("Radius must be positive.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var playerGrid = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(player.AttachedEntity).GridID;
            var mapGrid = mapManager.GetGrid(playerGrid);
            var playerPosition = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(player.AttachedEntity).Coordinates;
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();

            for (var i = -radius; i <= radius; i++)
            {
                for (var j = -radius; j <= radius; j++)
                {
                    var tile = mapGrid.GetTileRef(playerPosition.Offset((i, j)));
                    var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);
                    var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];

                    if (!tileDef.CanCrowbar) continue;

                    var underplating = tileDefinitionManager["underplating"];
                    mapGrid.SetTile(coordinates, new Robust.Shared.Map.Tile(underplating.TileId));
                }
            }
        }
    }
}
