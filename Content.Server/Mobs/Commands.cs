﻿using System.Text;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared.Jobs;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Mobs
{

    public class MindInfoCommand : IClientCommand
    {
        public string Command => "mindinfo";

        public string Description => "Lists info for the mind of a specific player.";

        public string Help => "mindinfo <session ID>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Expected exactly 1 argument.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetPlayerData(new NetSessionId(args[0]), out var data))
            {
                var mind = data.ContentData().Mind;

                var builder = new StringBuilder();
                builder.AppendFormat("player: {0}, mob: {1}\nroles: ", mind.SessionId, mind.OwnedMob?.Owner?.Uid);
                foreach (var role in mind.AllRoles)
                {
                    builder.AppendFormat("{0} ", role.Name);
                }

                shell.SendText(player, builder.ToString());
            }
            else
            {
                shell.SendText(player, "Can't find that mind");
            }
        }
    }

    public class AddRoleCommand : IClientCommand
    {
#pragma warning disable 649
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore 649

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
            if (mgr.TryGetPlayerData(new NetSessionId(args[0]), out var data))
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

    public class RemoveRoleCommand : IClientCommand
    {

#pragma warning disable 649
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public string Command => "rmrole";

        public string Description => "Removes a role from a player's mind.";

        public string Help => "rmrole <session ID> <Role Type>\nThat role type is the actual C# type name.";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 2)
            {
                shell.SendText(player, "Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetPlayerData(new NetSessionId(args[0]), out var data))
            {
                var mind = data.ContentData().Mind;
                var role = new Job(mind, _prototypeManager.Index<JobPrototype>(args[1]));
                mind.RemoveRole(role);
            }
            else
            {
                shell.SendText(player, "Can't find that mind");
            }
        }
    }
}
