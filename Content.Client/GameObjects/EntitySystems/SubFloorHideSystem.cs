﻿using Content.Client.GameObjects.Components;
using Content.Shared.Maps;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     Entity system backing <see cref="SubFloorHideComponent"/>.
    /// </summary>
    internal sealed class SubFloorHideSystem : EntitySystem
    {
        private bool _enableAll;

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
#pragma warning restore 649

        [ViewVariables(VVAccess.ReadWrite)]
        public bool EnableAll
        {
            get => _enableAll;
            set
            {
                _enableAll = value;

                UpdateAll();
            }
        }

        private void UpdateAll()
        {
            foreach (var comp in EntityManager.ComponentManager.EntityQuery<SubFloorHideComponent>())
            {
                var gridId = comp.Owner.Transform.GridID;
                var grid = _mapManager.GetGrid(gridId);

                var snapPos = comp.Owner.GetComponent<SnapGridComponent>();
                UpdateTile(grid, snapPos.Position);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            _mapManager.GridChanged += MapManagerOnGridChanged;
            _mapManager.TileChanged += MapManagerOnTileChanged;

            SubscribeLocalEvent<SubFloorHideDirtyEvent>(HandleDirtyEvent);
        }

        private void HandleDirtyEvent(SubFloorHideDirtyEvent ev)
        {
            if (!_mapManager.TryGetGrid(ev.Sender.Transform.GridID, out var grid))
            {
                return;
            }

            var indices = grid.WorldToTile(ev.Sender.Transform.WorldPosition);
            UpdateTile(grid, indices);
        }

        private void MapManagerOnTileChanged(object sender, TileChangedEventArgs e)
        {
            UpdateTile(_mapManager.GetGrid(e.NewTile.GridIndex), e.NewTile.GridIndices);
        }

        private void MapManagerOnGridChanged(object sender, GridChangedEventArgs e)
        {
            foreach (var modified in e.Modified)
            {
                UpdateTile(e.Grid, modified.position);
            }
        }

        private void UpdateTile(IMapGrid grid, MapIndices position)
        {
            var tile = grid.GetTileRef(position);
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
            foreach (var snapGridComponent in grid.GetSnapGridCell(position, SnapGridOffset.Center))
            {
                var entity = snapGridComponent.Owner;
                if (!entity.TryGetComponent(out SubFloorHideComponent subFloorComponent) ||
                    !entity.TryGetComponent(out ISpriteComponent spriteComponent))
                {
                    continue;
                }

                spriteComponent.Visible = EnableAll || !subFloorComponent.Running || tileDef.IsSubFloor;
            }
        }
    }
}
