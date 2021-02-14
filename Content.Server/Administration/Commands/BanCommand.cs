using System;
using System.Text;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

#nullable enable

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class BanCommand : IConsoleCommand
    {
        public string Command => "ban";
        public string Description => "Bans somebody";
        public string Help => $"Usage: {Command} <name or user ID> <reason> <duration in minutes, or 0 for permanent ban>";

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            var dbMan = IoCManager.Resolve<IServerDbManager>();

            string target;
            string reason;
            uint minutes;

            switch (args.Length)
            {
                case 2:
                    target = args[0];
                    reason = args[1];
                    minutes = 0;
                    break;
                case 3:
                    target = args[0];
                    reason = args[1];

                    if (!uint.TryParse(args[2], out minutes))
                    {
                        shell.WriteLine($"{args[2]} is not a valid amount of minutes.\n{Help}");
                        return;
                    }

                    break;
                default:
                    shell.WriteLine($"Invalid amount of arguments.{Help}");
                    return;
            }

            NetUserId targetUid;

            if (plyMgr.TryGetSessionByUsername(target, out var targetSession))
            {
                targetUid = targetSession.UserId;
            }
            else if (Guid.TryParse(target, out var targetGuid))
            {
                targetUid = new NetUserId(targetGuid);
            }
            else
            {
                shell.WriteLine("Unable to find user with that name.");
                return;
            }

            if (player != null && player.UserId == targetUid)
            {
                shell.WriteLine("You can't ban yourself!");
                return;
            }

            DateTimeOffset? expires = null;
            if (minutes > 0)
            {
                expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes);
            }

            await dbMan.AddServerBanAsync(new ServerBanDef(null, targetUid, null, DateTimeOffset.Now, expires, reason, player?.UserId, null));

            var response = new StringBuilder($"Banned {targetUid} with reason \"{reason}\"");

            response.Append(expires == null ?
                " permanently."
                : $" until {expires.ToString()}");

            shell.WriteLine(response.ToString());

            if (plyMgr.TryGetSessionById(targetUid, out var targetPlayer))
            {
                targetPlayer.ConnectedClient.Disconnect("You've been banned. Tough shit.");
            }
        }
    }
}
