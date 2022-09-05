using Content.Server.Administration;
using Content.Server.EUI;
using Content.Server.NPC.UI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.NPC.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class NPCCommand : IConsoleCommand
{
    public string Command => "npc";
    public string Description => "Opens the debug window for NPCs";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not IPlayerSession playerSession)
        {
            return;
        }

        var euiManager = IoCManager.Resolve<EuiManager>();
        euiManager.OpenEui(new NPCEui(), playerSession);
    }
}
