using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public class SetSolutionTemperature : IConsoleCommand
    {
        public string Command => "setsolutiontemperature";
        public string Description => "Set the temperature of some solution.";
        public string Help => $"Usage: {Command} <target> <solution> <new temperature>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
            {
                shell.WriteLine($"Not enough arguments.\n{Help}");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteLine($"Invalid entity id.");
                return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(uid, out SolutionContainerManagerComponent man))
            {
                shell.WriteLine($"Entity does not have any solutions.");
                return;
            }

            if (!man.Solutions.ContainsKey(args[1]))
            {
                var validSolutions = string.Join(", ", man.Solutions.Keys);
                shell.WriteLine($"Entity does not have a \"{args[1]}\" solution. Valid solutions are:\n{validSolutions}");
                return;
            }
            var solution = man.Solutions[args[1]];

            if (!float.TryParse(args[2], out var quantity))
            {
                shell.WriteLine($"Failed to parse new temperature.");
                return;
            }

            if (quantity <= 0.0f)
            {
                shell.WriteLine($"Cannot set the temperature of a solution to a non-positive number.");
                return;
            }

            EntitySystem.Get<SolutionContainerSystem>().SetTemperature(uid, solution, quantity);
        }
    }
}
