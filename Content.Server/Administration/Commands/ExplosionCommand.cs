using Content.Server.Explosion.EntitySystems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class ExplosionCommand : IConsoleCommand
    {
        public string Command => "explode";
        public string Description => "Train go boom";
        public string Help => "Usage: explode <x> <y> <dev> <heavy> <light> <flash>\n" +
                              "The explosion happens on the same map as the user.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity is not {Valid: true} playerEntity)
            {
                shell.WriteLine("You must have an attached entity.");
                return;
            }

            var x = float.Parse(args[0]);
            var y = float.Parse(args[1]);

            var dev = int.Parse(args[2]);
            var hvy = int.Parse(args[3]);
            var lgh = int.Parse(args[4]);
            var fla = int.Parse(args[5]);

            var mapTransform = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(playerEntity).GetMapTransform();
            var coords = new EntityCoordinates(mapTransform.Owner, x, y);

            EntitySystem.Get<ExplosionSystem>().SpawnExplosion(coords, dev, hvy, lgh, fla);
        }
    }
}
