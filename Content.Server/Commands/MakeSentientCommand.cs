#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public class MakeSentientCommand : IClientCommand
    {
        public string Command => "makesentient";
        public string Description => "Makes an entity sentient (able to be controlled by a player)";
        public string Help => "makesentient <entity id>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Wrong number of arguments.");
                return;
            }

            if (!int.TryParse(args[0], out var id))
            {
                shell.SendText(player, "Invalid argument.");
                return;
            }

            var entId = new EntityUid(id);

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(entId, out var entity) || entity.Deleted)
            {
                shell.SendText(player, "Invalid entity specified!");
                return;
            }

            if(entity.HasComponent<AiControllerComponent>())
                entity.RemoveComponent<AiControllerComponent>();

            entity.EnsureComponent<MindComponent>();
            entity.EnsureComponent<PlayerInputMoverComponent>();
        }
    }
}
