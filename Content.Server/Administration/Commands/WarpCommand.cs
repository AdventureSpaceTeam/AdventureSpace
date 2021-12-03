using System.Collections.Generic;
using System.Linq;
using Content.Server.Warps;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class WarpCommand : IConsoleCommand
    {
        public string Command => "warp";
        public string Description => "Teleports you to predefined areas on the map.";

        public string Help =>
            "warp <location>\nLocations you can teleport to are predefined by the map. " +
            "You can specify '?' as location to get a list of valid locations.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Only players can use this command");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine("Expected a single argument.");
                return;
            }

            var entMan = IoCManager.Resolve<IEntityManager>();
            var location = args[0];
            if (location == "?")
            {
                var locations = string.Join(", ",
                    entMan.EntityQuery<WarpPointComponent>(true)
                        .Select(p => p.Location)
                        .Where(p => p != null)
                        .OrderBy(p => p)
                        .Distinct());

                shell.WriteLine(locations);
            }
            else
            {
                if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                {
                    shell.WriteLine("You are not in-game!");
                    return;
                }

                var mapManager = IoCManager.Resolve<IMapManager>();
                var currentMap = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(player.AttachedEntity.Uid).MapID;
                var currentGrid = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(player.AttachedEntity.Uid).GridID;

                var found = entMan.EntityQuery<WarpPointComponent>(true)
                    .Where(p => p.Location == location)
                    .Select(p => IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(p.Owner.Uid).Coordinates)
                    .OrderBy(p => p, Comparer<EntityCoordinates>.Create((a, b) =>
                    {
                        // Sort so that warp points on the same grid/map are first.
                        // So if you have two maps loaded with the same warp points,
                        // it will prefer the warp points on the map you're currently on.
                        var aGrid = a.GetGridId(entMan);
                        var bGrid = b.GetGridId(entMan);

                        if (aGrid == bGrid)
                        {
                            return 0;
                        }

                        if (aGrid == currentGrid)
                        {
                            return -1;
                        }

                        if (bGrid == currentGrid)
                        {
                            return 1;
                        }

                        var mapA = mapManager.GetGrid(aGrid).ParentMapId;
                        var mapB = mapManager.GetGrid(bGrid).ParentMapId;

                        if (mapA == mapB)
                        {
                            return 0;
                        }

                        if (mapA == currentMap)
                        {
                            return -1;
                        }

                        if (mapB == currentMap)
                        {
                            return 1;
                        }

                        return 0;
                    }))
                    .FirstOrDefault();

                if (found.GetGridId(entMan) != GridId.Invalid)
                {
                    IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(player.AttachedEntity.Uid).Coordinates = found;
                    if (IoCManager.Resolve<IEntityManager>().TryGetComponent(player.AttachedEntity.Uid, out IPhysBody? physics))
                    {
                        physics.LinearVelocity = Vector2.Zero;
                    }
                }
                else
                {
                    shell.WriteLine("That location does not exist!");
                }
            }
        }
    }
}
