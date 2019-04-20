﻿using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    public class CrowbarComponent : ToolComponent, IAfterAttack
    {
#pragma warning disable 649
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        /// <summary>
        /// Tool that can be used to crowbar things apart, such as deconstructing
        /// </summary>
        public override string Name => "Crowbar";

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);
            var tile = mapGrid.GetTile(eventArgs.ClickLocation);
            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];
            if (tileDef.CanCrowbar)
            {
                var underplating = _tileDefinitionManager["underplating"];
                mapGrid.SetTile(eventArgs.ClickLocation, underplating.TileId);
               _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/crowbar.ogg", Owner);
            }
        }
    }
}
