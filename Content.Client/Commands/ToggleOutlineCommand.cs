using Content.Shared;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public class ToggleOutlineCommand : IConsoleCommand
    {
        public string Command => "toggleoutline";

        public string Description => "Toggles outline drawing on entities.";

        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var configurationManager = IoCManager.Resolve<IConfigurationManager>();
            var cvar = CCVars.OutlineEnabled;
            var old = configurationManager.GetCVar(cvar);

            configurationManager.SetCVar(cvar, !old);
            shell.WriteLine($"Draw outlines set to: {configurationManager.GetCVar(cvar)}");
        }
    }
}
