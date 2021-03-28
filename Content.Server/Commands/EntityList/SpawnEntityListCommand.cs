﻿using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Prototypes.EntityList;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Commands.EntityList
{
    [AdminCommand(AdminFlags.Spawn)]
    public class SpawnEntityListCommand : IConsoleCommand
    {
        public string Command => "spawnentitylist";
        public string Description => "Spawns a list of entities around you";
        public string Help => $"Usage: {Command} <entityListPrototypeId>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError($"Invalid arguments.\n{Help}");
                return;
            }

            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteError("You must be a player to run this command.");
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.WriteError("You must have an entity to run this command.");
                return;
            }

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            if (!prototypeManager.TryIndex(args[0], out EntityListPrototype? prototype))
            {
                shell.WriteError($"No {nameof(EntityListPrototype)} found with id {args[0]}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var i = 0;

            foreach (var entity in prototype.Entities(prototypeManager))
            {
                entityManager.SpawnEntity(entity.ID, player.AttachedEntity.Transform.Coordinates);
                i++;
            }

            shell.WriteLine($"Spawned {i} entities.");
        }
    }
}
