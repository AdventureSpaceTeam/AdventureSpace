using System.Collections.Generic;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Mobs
{
    [UsedImplicitly]
    public sealed class NearbyPlayersState : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyPlayers";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent controller))
            {
                return result;
            }

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var nearbyPlayers = playerManager.GetPlayersInRange(Owner.Transform.GridPosition, (int) controller.VisionRadius);

            foreach (var player in nearbyPlayers)
            {
                if (player.AttachedEntity != Owner && player.AttachedEntity.HasComponent<SpeciesComponent>())
                {
                    result.Add(player.AttachedEntity);
                }
            }

            return result;
        }
    }
}
