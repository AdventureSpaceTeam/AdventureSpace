using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    internal class CablePlacerComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <inheritdoc />
        public override string Name => "CablePlacer";

        [ViewVariables]
        [DataField("cablePrototypeID")]
        private string? _cablePrototypeID = "CableHV";

        [ViewVariables]
        [DataField("blockingWireType")]
        private CableType _blockingCableType = CableType.HighVoltage;

        /// <inheritdoc />
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User.Uid))
                return false;

            if (_cablePrototypeID == null)
                return false;

            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return false;

            if(!_mapManager.TryGetGrid(eventArgs.ClickLocation.GetGridId(IoCManager.Resolve<IEntityManager>()), out var grid))
                return false;

            var snapPos = grid.TileIndicesFor(eventArgs.ClickLocation);
            var tileDef = grid.GetTileRef(snapPos).Tile.GetContentTileDefinition();

            if(!tileDef.IsSubFloor || !tileDef.Sturdy)
                return false;

            foreach (var anchored in grid.GetAnchoredEntities(snapPos))
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent<CableComponent>(anchored, out var wire) && wire.CableType == _blockingCableType)
                {
                    return false;
                }
            }

            if (Owner.TryGetComponent<StackComponent>(out var stack)
                && !EntitySystem.Get<StackSystem>().Use(Owner.Uid, 1, stack))
                return false;

            IoCManager.Resolve<IEntityManager>().SpawnEntity(_cablePrototypeID, grid.GridTileToLocal(snapPos));
            return true;
        }
    }
}
