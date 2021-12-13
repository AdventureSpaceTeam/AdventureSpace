using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;


namespace Content.Server.Administration.Commands
{
    [AnyCommand]
    public class ReAdminCommand : IConsoleCommand
    {
        public string Command => "readmin";
        public string Description => "Re-admins you if you previously de-adminned.";
        public string Help => "Usage: readmin";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You cannot use this command from the server console.");
                return;
            }

            var mgr = IoCManager.Resolve<IAdminManager>();

            if (mgr.GetAdminData(player, includeDeAdmin: true) == null)
            {
                shell.WriteLine("You're not an admin.");
                return;
            }

            mgr.ReAdmin(player);
        }
    }
}
