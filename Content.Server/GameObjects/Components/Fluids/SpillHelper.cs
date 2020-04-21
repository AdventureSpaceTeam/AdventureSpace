using Content.Shared.Chemistry;
using Robust.Server.Interfaces.GameObjects;

using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Fluids
{
    public static class SpillHelper
    {

        /// <summary>
        /// Spills the specified solution at the entity's location if possible.
        /// </summary>
        /// <param name="entity">Entity location to spill at</param>
        /// <param name="solution">Initial solution for the prototype</param>
        /// <param name="prototype">Prototype to use</param>
        internal static void SpillAt(IEntity entity, Solution solution, string prototype)
        {
            var entityLocation = entity.Transform.GridPosition;
            SpillAt(entityLocation, solution, prototype);
        }

        // Other functions will be calling this one

        /// <summary>
        /// Spills solution at the specified grid co-ordinates
        /// </summary>
        /// <param name="gridCoordinates"></param>
        /// <param name="solution">Initial solution for the prototype</param>
        /// <param name="prototype">Prototype to use</param>
        internal static void SpillAt(GridCoordinates gridCoordinates, Solution solution, string prototype)
        {
            if (solution.TotalVolume == 0)
            {
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();

            var mapGrid = mapManager.GetGrid(gridCoordinates.GridID);

            // If space return early, let that spill go out into the void
            var tileRef = mapGrid.GetTileRef(gridCoordinates);
            if (tileRef.Tile.IsEmpty)
            {
                return;
            }

            // Get normalized co-ordinate for spill location and spill it in the centre
            // TODO: Does SnapGrid or something else already do this?
            var spillTileMapGrid = mapManager.GetGrid(gridCoordinates.GridID);
            var spillTileRef = spillTileMapGrid.GetTileRef(gridCoordinates).GridIndices;
            var spillGridCoords = spillTileMapGrid.GridTileToLocal(spillTileRef);

            var spilt = false;

            foreach (var spillEntity in entityManager.GetEntitiesAt(spillTileMapGrid.ParentMapId, spillGridCoords.Position))
            {
                if (!spillEntity.TryGetComponent(out PuddleComponent puddleComponent))
                {
                    continue;
                }

                if (!puddleComponent.TryAddSolution(solution))
                {
                    continue;
                }

                spilt = true;
                break;
            }

            // Did we add to an existing puddle
            if (spilt)
            {
                return;
            }

            var puddle = serverEntityManager.SpawnEntity(prototype, spillGridCoords);
            puddle.GetComponent<PuddleComponent>().TryAddSolution(solution);
        }

    }

}
