using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Rounding;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

// ReSharper disable once RedundantUsingDirective

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Robust.Shared.IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [Robust.Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        /// <summary>
        ///     The tiles that have had their atmos data updated since last tick
        /// </summary>
        private readonly Dictionary<EntityUid, HashSet<Vector2i>> _invalidTiles = new();

        private readonly Dictionary<IPlayerSession, PlayerGasOverlay> _knownPlayerChunks =
            new();

        /// <summary>
        ///     Gas data stored in chunks to make PVS / bubbling easier.
        /// </summary>
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, GasOverlayChunk>> _overlay =
            new();

        /// <summary>
        ///     How far away do we update gas overlays (minimum; due to chunking further away tiles may also be updated).
        /// </summary>
        private float _updateRange;

        // Because the gas overlay updates aren't run every tick we need to avoid the pop-in that might occur with
        // the regular PVS range.
        private const float RangeOffset = 6.0f;

        /// <summary>
        ///     Overlay update ticks per second.
        /// </summary>
        private float _updateCooldown;

        private int _thresholds;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CCVars.NetGasOverlayTickRate, value => _updateCooldown = value > 0.0f ? 1 / value : float.MaxValue, true);
            configManager.OnValueChanged(CVars.NetMaxUpdateRange, value => _updateRange = value + RangeOffset, true);
            configManager.OnValueChanged(CCVars.GasOverlayThresholds, value => _thresholds = value, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invalidate(EntityUid gridIndex, Vector2i indices)
        {
            if (!_invalidTiles.TryGetValue(gridIndex, out var existing))
            {
                existing = new HashSet<Vector2i>();
                _invalidTiles[gridIndex] = existing;
            }

            existing.Add(indices);
        }

        private GasOverlayChunk GetOrCreateChunk(EntityUid gridIndex, Vector2i indices)
        {
            if (!_overlay.TryGetValue(gridIndex, out var chunks))
            {
                chunks = new Dictionary<Vector2i, GasOverlayChunk>();
                _overlay[gridIndex] = chunks;
            }

            var chunkIndices = GetGasChunkIndices(indices);

            if (!chunks.TryGetValue(chunkIndices, out var chunk))
            {
                chunk = new GasOverlayChunk(gridIndex, chunkIndices);
                chunks[chunkIndices] = chunk;
            }

            return chunk;
        }

        private void OnGridRemoved(GridRemovalEvent ev)
        {
            _overlay.Remove(ev.EntityUid);
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame)
            {
                if (_knownPlayerChunks.ContainsKey(e.Session))
                {
                    _knownPlayerChunks.Remove(e.Session);
                }

                return;
            }

            if (!_knownPlayerChunks.ContainsKey(e.Session))
            {
                _knownPlayerChunks[e.Session] = new PlayerGasOverlay();
            }
        }

        /// <summary>
        ///     Checks whether the overlay-relevant data for a gas tile has been updated.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="oldTile"></param>
        /// <param name="indices"></param>
        /// <param name="overlayData"></param>
        /// <returns>true if updated</returns>
        private bool TryRefreshTile(GridAtmosphereComponent gridAtmosphere, GasOverlayData oldTile, Vector2i indices, out GasOverlayData overlayData)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(indices, out var tile))
            {
                overlayData = default;
                return false;
            }

            var tileData = new List<GasData>();

            if(tile.Air != null)
                for (byte i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    var gas = _atmosphereSystem.GetGas(i);
                    var overlay = _atmosphereSystem.GetOverlay(i);
                    if (overlay == null) continue;

                    var moles = tile.Air.Moles[i];

                    if (moles < gas.GasMolesVisible) continue;

                    var opacity = (byte) (ContentHelpers.RoundToLevels(MathHelper.Clamp01(moles / gas.GasMolesVisibleMax) * 255, byte.MaxValue, _thresholds) * 255 / (_thresholds - 1));
                    var data = new GasData(i, opacity);
                    tileData.Add(data);
                }

            overlayData = new GasOverlayData(tile!.Hotspot.State, tile.Hotspot.Temperature, tileData.Count == 0 ? Array.Empty<GasData>() : tileData.ToArray());

            if (overlayData.Equals(oldTile))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Get every chunk in range of our entity that exists, including on other grids.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private List<GasOverlayChunk> GetChunksInRange(EntityUid entity)
        {
            // This is the max in any direction that we can get a chunk (e.g. max 2 chunks away of data).
            var (maxXDiff, maxYDiff) = ((int) (_updateRange / ChunkSize) + 1, (int) (_updateRange / ChunkSize) + 1);

            // Setting initial list size based on the theoretical max number of chunks from a single grid. For default
            // parameters, this is currently 6^2 = 36. Unless a player is near more than one grid, this is will
            // generally slightly over-estimate the actual list size, which will be either 25, 30, or 36 (assuming the
            // player is not near the edge of a grid).
            var initialListSize = (1 + (int) MathF.Ceiling(2 * _updateRange / ChunkSize)) * (1 + (int) MathF.Ceiling(2 * _updateRange / ChunkSize));

            var inRange = new List<GasOverlayChunk>(initialListSize);

            var xform = Transform(entity);
            var worldPos = xform.MapPosition;

            var worldBounds = Box2.CenteredAround(worldPos.Position, new Vector2(_updateRange, _updateRange));

            foreach (var grid in _mapManager.FindGridsIntersecting(xform.MapID, worldBounds))
            {
                if (!_overlay.TryGetValue(grid.GridEntityId, out var chunks))
                {
                    continue;
                }

                var entityTile = grid.GetTileRef(worldPos).GridIndices;

                for (var x = -maxXDiff; x <= maxXDiff; x++)
                {
                    for (var y = -maxYDiff; y <= maxYDiff; y++)
                    {
                        var chunkIndices = GetGasChunkIndices(new Vector2i(entityTile.X + x * ChunkSize, entityTile.Y + y * ChunkSize));

                        if (!chunks.TryGetValue(chunkIndices, out var chunk)) continue;

                        // Now we'll check if it's in range and relevant for us
                        // (e.g. if we're on the very edge of a chunk we may need more chunks).

                        var (xDiff, yDiff) = (chunkIndices.X - entityTile.X, chunkIndices.Y - entityTile.Y);
                        if (xDiff > _updateRange ||
                            yDiff > _updateRange ||
                            xDiff < 0 && Math.Abs(xDiff + ChunkSize) > _updateRange ||
                            yDiff < 0 && Math.Abs(yDiff + ChunkSize) > _updateRange) continue;

                        inRange.Add(chunk);
                    }
                }
            }

            return inRange;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            AccumulatedFrameTime += frameTime;

            if (AccumulatedFrameTime < _updateCooldown) return;

            // TODO: So in the worst case scenario we still have to send a LOT of tile data per tick if there's a fire.
            // If we go with say 15 tile radius then we have up to 900 tiles to update per tick.
            // In a saltern fire the worst you'll normally see is around 650 at the moment.
            // Need a way to fake this more because sending almost 2,000 tile updates per second to even 50 players is... yikes
            // I mean that's as big as it gets so larger maps will have the same but still, that's a lot of data.

            // Some ways to do this are potentially: splitting fire and gas update data so they don't update at the same time
            // (gives the illusion of more updates happening), e.g. if gas updates are 3 times a second and fires are 1.6 times a second or something.
            // Could also look at updating tiles close to us more frequently (e.g. within 1 chunk every tick).
            // Stuff just out of our viewport we need so when we move it doesn't pop in but it doesn't mean we need to update it every tick.

            AccumulatedFrameTime -= _updateCooldown;

            var gridAtmosComponents = new Dictionary<EntityUid, GridAtmosphereComponent>();
            var updatedTiles = new Dictionary<GasOverlayChunk, HashSet<Vector2i>>();

            // So up to this point we've been caching the updated tiles for multiple ticks.
            // Now we'll go through and check whether the update actually matters for the overlay or not,
            // and if not then we won't bother sending the data.
            foreach (var (gridId, indices) in _invalidTiles)
            {
                if (!_mapManager.TryGetGrid(gridId, out var grid))
                {
                    return;
                }

                var gridEntityId = grid.GridEntityId;

                if (!EntityManager.TryGetComponent(gridEntityId, out GridAtmosphereComponent? gam))
                {
                    continue;
                }

                // If it's being invalidated it should have this right?
                // At any rate we'll cache it for here + the AddChunk
                if (!gridAtmosComponents.ContainsKey(gridId))
                {
                    gridAtmosComponents[gridId] = gam;
                }

                foreach (var invalid in indices.ToArray())
                {
                    var chunk = GetOrCreateChunk(gridId, invalid);

                    if (!TryRefreshTile(gam, chunk.GetData(invalid), invalid, out var data)) continue;

                    if (!updatedTiles.TryGetValue(chunk, out var tiles))
                    {
                        tiles = new HashSet<Vector2i>();
                        updatedTiles[chunk] = tiles;
                    }

                    tiles.Add(invalid);
                    chunk.Update(data, invalid);
                }
            }

            var currentTick = _gameTiming.CurTick;

            // Set the LastUpdate for chunks.
            foreach (var (chunk, _) in updatedTiles)
            {
                chunk.Dirty(currentTick);
            }

            // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
            // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
            // Afterwards we reset all the chunk data for the next time we tick.
            foreach (var (session, overlay) in _knownPlayerChunks)
            {
                if (session.AttachedEntity is not {Valid: true} entity) continue;

                // Get chunks in range and update if we've moved around or the chunks have new overlay data
                var chunksInRange = GetChunksInRange(entity);
                var knownChunks = overlay.GetKnownChunks();
                var chunksToRemove = new List<GasOverlayChunk>();
                var chunksToAdd = new List<GasOverlayChunk>();

                foreach (var chunk in chunksInRange)
                {
                    if (!knownChunks.Contains(chunk))
                    {
                        chunksToAdd.Add(chunk);
                    }
                }

                foreach (var chunk in knownChunks)
                {
                    if (!chunksInRange.Contains(chunk))
                    {
                        chunksToRemove.Add(chunk);
                    }
                }

                foreach (var chunk in chunksToAdd)
                {
                    var message = overlay.AddChunk(currentTick, chunk);
                    if (message != null)
                    {
                        RaiseNetworkEvent(message, session.ConnectedClient);
                    }
                }

                foreach (var chunk in chunksToRemove)
                {
                    overlay.RemoveChunk(chunk);
                }

                var clientInvalids = new Dictionary<EntityUid, List<(Vector2i, GasOverlayData)>>();

                // Check for any dirty chunks in range and bundle the data to send to the client.
                foreach (var chunk in chunksInRange)
                {
                    if (!updatedTiles.TryGetValue(chunk, out var invalids)) continue;

                    if (!clientInvalids.TryGetValue(chunk.GridIndices, out var existingData))
                    {
                        existingData = new List<(Vector2i, GasOverlayData)>();
                        clientInvalids[chunk.GridIndices] = existingData;
                    }

                    chunk.GetData(existingData, invalids);
                }

                foreach (var (grid, data) in clientInvalids)
                {
                    RaiseNetworkEvent(overlay.UpdateClient(grid, data), session.ConnectedClient);
                }
            }

            // Cleanup
            _invalidTiles.Clear();
        }
        private sealed class PlayerGasOverlay
        {
            private readonly Dictionary<EntityUid, Dictionary<Vector2i, GasOverlayChunk>> _data =
                new();

            private readonly Dictionary<GasOverlayChunk, GameTick> _lastSent =
                new();

            public GasOverlayMessage UpdateClient(EntityUid grid, List<(Vector2i, GasOverlayData)> data)
            {
                return new(grid, data);
            }

            public void Reset()
            {
                _data.Clear();
                _lastSent.Clear();
            }

            public List<GasOverlayChunk> GetKnownChunks()
            {
                var known = new List<GasOverlayChunk>();

                foreach (var (_, chunks) in _data)
                {
                    foreach (var (_, chunk) in chunks)
                    {
                        known.Add(chunk);
                    }
                }

                return known;
            }

            public GasOverlayMessage? AddChunk(GameTick currentTick, GasOverlayChunk chunk)
            {
                if (!_data.TryGetValue(chunk.GridIndices, out var chunks))
                {
                    chunks = new Dictionary<Vector2i, GasOverlayChunk>();
                    _data[chunk.GridIndices] = chunks;
                }

                if (_lastSent.TryGetValue(chunk, out var last) && last >= chunk.LastUpdate)
                {
                    return null;
                }

                _lastSent[chunk] = currentTick;
                var message = ChunkToMessage(chunk);

                return message;
            }

            public void RemoveChunk(GasOverlayChunk chunk)
            {
                // Don't need to sync to client as they can manage it themself.
                if (!_data.TryGetValue(chunk.GridIndices, out var chunks))
                {
                    return;
                }

                if (chunks.ContainsKey(chunk.Vector2i))
                {
                    chunks.Remove(chunk.Vector2i);
                }
            }

            /// <summary>
            ///     Retrieve a whole chunk as a message, only getting the relevant tiles for the gas overlay.
            /// </summary>
            /// <param name="chunk"></param>
            /// <returns></returns>
            private GasOverlayMessage? ChunkToMessage(GasOverlayChunk chunk)
            {
                // Chunk data should already be up to date.
                // Only send relevant tiles to client.

                var tileData = new List<(Vector2i, GasOverlayData)>();

                for (var x = 0; x < ChunkSize; x++)
                {
                    for (var y = 0; y < ChunkSize; y++)
                    {
                        // TODO: Check could be more robust I think.
                        var data = chunk.TileData[x, y];
                        if ((data.Gas == null || data.Gas.Length == 0) && data.FireState == 0 && data.FireTemperature == 0.0f)
                        {
                            continue;
                        }

                        var indices = new Vector2i(chunk.Vector2i.X + x, chunk.Vector2i.Y + y);
                        tileData.Add((indices, data));
                    }
                }

                if (tileData.Count == 0)
                {
                    return null;
                }

                return new GasOverlayMessage(chunk.GridIndices, tileData);
            }
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _invalidTiles.Clear();
            _overlay.Clear();

            foreach (var (_, data) in _knownPlayerChunks)
            {
                data.Reset();
            }
        }
    }
}
