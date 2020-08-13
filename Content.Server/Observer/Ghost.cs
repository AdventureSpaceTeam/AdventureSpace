using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Observer
{
    public class Ghost : IClientCommand
    {
        public string Command => "ghost";
        public string Description => "Give up on life and become a ghost.";
        public string Help => "ghost";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText((IPlayerSession) null, "Nah");
                return;
            }

            var mind = player.ContentData().Mind;
            var canReturn = player.AttachedEntity != null;
            var name = player.AttachedEntity?.Name ?? player.Name;

            if (player.AttachedEntity != null && player.AttachedEntity.HasComponent<GhostComponent>())
                return;

            if (mind.VisitingEntity != null)
            {
                mind.UnVisit();
                mind.VisitingEntity.Delete();
            }

            var position = player.AttachedEntity?.Transform.GridPosition ?? IoCManager.Resolve<IGameTicker>().GetObserverSpawnPoint();



            if (canReturn && player.AttachedEntity.TryGetComponent(out SpeciesComponent species))
            {
                switch (species.CurrentDamageState)
                {
                    case DeadState _:
                        canReturn = true;
                        break;
                    case CriticalState _:
                        canReturn = true;
                        if (!player.AttachedEntity.TryGetComponent(out DamageableComponent damageable)) break;
                        damageable.TakeDamage(DamageType.Total, 100); // TODO: Use airloss/oxyloss instead
                        break;
                    default:
                        canReturn = false;
                        break;
                }
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var ghost = entityManager.SpawnEntity("MobObserver", position);
            ghost.Name = mind.CharacterName;

            var ghostComponent = ghost.GetComponent<GhostComponent>();
            ghostComponent.CanReturnToBody = canReturn;

            if(canReturn)
                mind.Visit(ghost);
            else
                mind.TransferTo(ghost);
        }
    }
}
