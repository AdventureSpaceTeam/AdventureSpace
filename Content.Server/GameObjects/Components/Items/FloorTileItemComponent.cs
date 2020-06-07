﻿using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class FloorTileItemComponent : Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override string Name => "FloorTile";
        private StackComponent _stack;
        private string _outputTile;


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _outputTile, "output", "floor_steel");
        }

        public override void Initialize()
        {
            base.Initialize();
            _stack = Owner.GetComponent<StackComponent>();
        }
        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!InteractionChecks.InRangeUnobstructed(eventArgs)) return;

            var attacked = eventArgs.Target;
            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);
            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            if (tileDef.IsSubFloor && attacked == null && _stack.Use(1))
            {
                var desiredTile = _tileDefinitionManager[_outputTile];
                mapGrid.SetTile(eventArgs.ClickLocation, new Tile(desiredTile.TileId));
                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/items/genhit.ogg", eventArgs.ClickLocation);
                if(_stack.Count < 1){
                    Owner.Delete();
                }
            }


        }

    }
}
