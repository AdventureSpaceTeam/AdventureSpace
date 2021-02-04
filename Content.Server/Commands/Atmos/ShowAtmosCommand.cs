#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems.Atmos;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class ShowAtmos : IConsoleCommand
    {
        public string Command => "showatmos";
        public string Description => "Toggles seeing atmos debug overlay.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You must be a player to use this command.");
                return;
            }

            var atmosDebug = EntitySystem.Get<AtmosDebugOverlaySystem>();
            var enabled = atmosDebug.ToggleObserver(player);

            shell.WriteLine(enabled
                ? "Enabled the atmospherics debug overlay."
                : "Disabled the atmospherics debug overlay.");
        }
    }
}
