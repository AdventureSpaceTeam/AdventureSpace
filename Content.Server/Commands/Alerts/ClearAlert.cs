#nullable enable
using System;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Alerts
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class ClearAlert : IConsoleCommand
    {
        public string Command => "clearalert";
        public string Description => "Clears an alert for a player, defaulting to current player";
        public string Help => "clearalert <alertType> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine("You don't have an entity.");
                return;
            }

            var attachedEntity = player.AttachedEntity;

            if (args.Length > 1)
            {
                var target = args[1];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (!attachedEntity.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                shell.WriteLine("user has no alerts component");
                return;
            }

            var alertType = args[0];
            var alertMgr = IoCManager.Resolve<AlertManager>();
            if (!alertMgr.TryGet(Enum.Parse<AlertType>(alertType), out var alert))
            {
                shell.WriteLine("unrecognized alertType " + alertType);
                return;
            }

            alertsComponent.ClearAlert(alert.AlertType);
        }
    }
}
