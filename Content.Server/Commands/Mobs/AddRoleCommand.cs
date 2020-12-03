﻿using Content.Server.Administration;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Commands.Mobs
{
    [AdminCommand(AdminFlags.Fun)]
    public class AddRoleCommand : IClientCommand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "addrole";

        public string Description => "Adds a role to a player's mind.";

        public string Help => "addrole <session ID> <Role Type>\nThat role type is the actual C# type name.";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 2)
            {
                shell.SendText(player, "Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                var mind = data.ContentData().Mind;
                var role = new Job(mind, _prototypeManager.Index<JobPrototype>(args[1]));
                mind.AddRole(role);
            }
            else
            {
                shell.SendText(player, "Can't find that mind");
            }
        }
    }
}