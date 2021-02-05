#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Mobs.Speech;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Mobs.Speech;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public class MakeSentientCommand : IConsoleCommand
    {
        public string Command => "makesentient";
        public string Description => "Makes an entity sentient (able to be controlled by a player)";
        public string Help => "makesentient <entity id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Wrong number of arguments.");
                return;
            }

            if (!int.TryParse(args[0], out var id))
            {
                shell.WriteLine("Invalid argument.");
                return;
            }

            var entId = new EntityUid(id);

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(entId, out var entity) || entity.Deleted)
            {
                shell.WriteLine("Invalid entity specified!");
                return;
            }

            if(entity.HasComponent<AiControllerComponent>())
                entity.RemoveComponent<AiControllerComponent>();

            entity.EnsureComponent<MindComponent>();
            entity.EnsureComponent<PlayerInputMoverComponent>();
            entity.EnsureComponent<SharedSpeechComponent>();
            entity.EnsureComponent<SharedEmotingComponent>();
        }
    }
}
