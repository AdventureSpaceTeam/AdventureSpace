using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems
{
    /// <summary>
    ///     This is our SSAir equivalent, if you need to interact with or query atmos in any way, go through this.
    /// </summary>
    [UsedImplicitly]
    public partial class AtmosphereSystem : SharedAtmosphereSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private const float ExposedUpdateDelay = 1f;
        private float _exposedTimer = 0f;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(NodeGroupSystem));

            InitializeGases();
            InitializeCVars();
            InitializeGrid();

            #region Events

            // Map events.
            _mapManager.TileChanged += OnTileChanged;

            #endregion
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.TileChanged -= OnTileChanged;
        }

        private void OnTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // When a tile changes, we want to update it only if it's gone from
            // space -> not space or vice versa. So if the old tile is the
            // same as the new tile in terms of space-ness, ignore the change

            if (eventArgs.NewTile.IsSpace() == eventArgs.OldTile.IsSpace())
            {
                return;
            }

            InvalidateTile(eventArgs.NewTile.GridIndex, eventArgs.NewTile.GridIndices);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProcessing(frameTime);

            _exposedTimer += frameTime;

            if (_exposedTimer >= ExposedUpdateDelay)
            {
                foreach (var exposed in EntityManager.ComponentManager.EntityQuery<AtmosExposedComponent>())
                {
                    // TODO ATMOS: Kill this with fire.
                    var tile = GetTileAtmosphereOrCreateSpace(exposed.Owner.Transform.Coordinates);
                    if (tile == null) continue;
                    exposed.Update(tile, _exposedTimer, this);
                }

                _exposedTimer -= ExposedUpdateDelay;
            }
        }
    }
}
