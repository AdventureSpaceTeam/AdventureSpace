﻿#nullable enable
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class DeleteComponent : IConsoleCommand
    {
        public string Command => "deletecomponent";
        public string Description => "Deletes all instances of the specified component.";
        public string Help => $"Usage: {Command} <name>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    shell.WriteLine($"Not enough arguments.\n{Help}");
                    break;
                default:
                    var name = string.Join(" ", args);
                    var componentFactory = IoCManager.Resolve<IComponentFactory>();
                    var entityManager = IoCManager.Resolve<IEntityManager>();

                    if (!componentFactory.TryGetRegistration(name, out var registration))
                    {
                        shell.WriteLine($"No component exists with name {name}.");
                        break;
                    }

                    var componentType = registration.Type;
                    var components = entityManager.ComponentManager.GetAllComponents(componentType, true);

                    var i = 0;

                    foreach (var component in components)
                    {
                        var uid = component.Owner.Uid;
                        entityManager.ComponentManager.RemoveComponent(uid, component);
                        i++;
                    }

                    shell.WriteLine($"Removed {i} components with name {name}.");

                    break;
            }
        }
    }
}
