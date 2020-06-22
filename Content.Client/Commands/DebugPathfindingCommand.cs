using Content.Client.GameObjects.EntitySystems.AI;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class DebugPathfindingCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "pathfinder";
        public string Description => "Toggles visibility of pathfinding debuggers.";
        public string Help => "pathfinder [hide/nodes/routes/graph]";

        public bool Execute(IDebugConsole console, params string[] args)
        {
#if DEBUG
            if (args.Length < 1)
            {
                return true;
            }

            var anyAction = false;
            var debugSystem = EntitySystem.Get<ClientPathfindingDebugSystem>();

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "hide":
                        debugSystem.Disable();
                        anyAction = true;
                        break;
                    // Shows all nodes on the closed list
                    case "nodes":
                        debugSystem.ToggleTooltip(PathfindingDebugMode.Nodes);
                        anyAction = true;
                        break;
                    // Will show just the constructed route
                    case "routes":
                        debugSystem.ToggleTooltip(PathfindingDebugMode.Route);
                        anyAction = true;
                        break;
                    // Shows all of the pathfinding chunks
                    case "graph":
                        debugSystem.ToggleTooltip(PathfindingDebugMode.Graph);
                        anyAction = true;
                        break;
                    default:
                        continue;
                }
            }

            return !anyAction;
#else
            return true;
#endif
        }
    }
}
