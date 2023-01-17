using Content.Client.Decals.Overlays;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Client.Decals
{
    public sealed class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly SpriteSystem _sprites = default!;

        private DecalOverlay _overlay = default!;

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(_sprites, EntityManager, PrototypeManager);
            _overlayManager.AddOverlay(_overlay);

            SubscribeLocalEvent<DecalGridComponent, ComponentHandleState>(OnHandleState);
            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
        }

        public void ToggleOverlay()
        {
            if (_overlayManager.HasOverlay<DecalOverlay>())
            {
                _overlayManager.RemoveOverlay(_overlay);
            }
            else
            {
                _overlayManager.AddOverlay(_overlay);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlayManager.RemoveOverlay(_overlay);
        }

        protected override void OnDecalRemoved(EntityUid gridId, uint decalId, DecalGridComponent component, Vector2i indices, DecalChunk chunk)
        {
            base.OnDecalRemoved(gridId, decalId, component, indices, chunk);

            if (!component.DecalZIndexIndex.Remove(decalId, out var zIndex))
                return;

            if (!component.DecalRenderIndex.TryGetValue(zIndex, out var renderIndex))
                return;

            renderIndex.Remove(decalId);
            if (renderIndex.Count == 0)
                component.DecalRenderIndex.Remove(zIndex);
        }

        private void OnHandleState(EntityUid gridUid, DecalGridComponent gridComp, ref ComponentHandleState args)
        {
            if (args.Current is not DecalGridState state)
                return;

            // is this a delta or full state?
            var removedChunks = new List<Vector2i>();
            if (!state.FullState)
            {
                foreach (var key in gridComp.ChunkCollection.ChunkCollection.Keys)
                {
                    if (!state.AllChunks!.Contains(key))
                        removedChunks.Add(key);
                }
            }
            else
            {
                foreach (var key in gridComp.ChunkCollection.ChunkCollection.Keys)
                {
                    if (!state.Chunks.ContainsKey(key))
                        removedChunks.Add(key);
                }
            }

            if (removedChunks.Count > 0)
                RemoveChunks(gridUid, gridComp, removedChunks);

            if (state.Chunks.Count > 0)
                UpdateChunks(gridUid, gridComp, state.Chunks);
        }

        private void OnChunkUpdate(DecalChunkUpdateEvent ev)
        {
            foreach (var (gridId, updatedGridChunks) in ev.Data)
            {
                if (updatedGridChunks.Count == 0) continue;

                if (!TryComp(gridId, out DecalGridComponent? gridComp))
                {
                    Logger.Error($"Received decal information for an entity without a decal component: {ToPrettyString(gridId)}");
                    continue;
                }

                UpdateChunks(gridId, gridComp, updatedGridChunks);
            }

            // Now we'll cull old chunks out of range as the server will send them to us anyway.
            foreach (var (gridId, chunks) in ev.RemovedChunks)
            {
                if (chunks.Count == 0) continue;

                if (!TryComp(gridId, out DecalGridComponent? gridComp))
                {
                    Logger.Error($"Received decal information for an entity without a decal component: {ToPrettyString(gridId)}");
                    continue;
                }

                RemoveChunks(gridId, gridComp, chunks);
            }
        }

        private void UpdateChunks(EntityUid gridId, DecalGridComponent gridComp, Dictionary<Vector2i, DecalChunk> updatedGridChunks)
        {
            var chunkCollection = gridComp.ChunkCollection.ChunkCollection;
            var renderIndex = gridComp.DecalRenderIndex;
            var zIndexIndex = gridComp.DecalZIndexIndex;

            // Update any existing data / remove decals we didn't receive data for.
            foreach (var (indices, newChunkData) in updatedGridChunks)
            {
                if (chunkCollection.TryGetValue(indices, out var chunk))
                {
                    var removedUids = new HashSet<uint>(chunk.Decals.Keys);
                    removedUids.ExceptWith(newChunkData.Decals.Keys);
                    foreach (var removedUid in removedUids)
                    {
                        OnDecalRemoved(gridId, removedUid, gridComp, indices, chunk);
                        gridComp.DecalIndex.Remove(removedUid);
                    }
                }

                chunkCollection[indices] = newChunkData;

                foreach (var (uid, decal) in newChunkData.Decals)
                {
                    if (zIndexIndex.TryGetValue(uid, out var zIndex))
                        renderIndex[zIndex].Remove(uid);

                    renderIndex.GetOrNew(decal.ZIndex)[uid] = decal;
                    zIndexIndex[uid] = decal.ZIndex;
                    gridComp.DecalIndex[uid] = indices;
                }
            }
        }

        private void RemoveChunks(EntityUid gridId, DecalGridComponent gridComp, IEnumerable<Vector2i> chunks)
        {
            var chunkCollection = gridComp.ChunkCollection.ChunkCollection;

            foreach (var index in chunks)
            {
                if (!chunkCollection.TryGetValue(index, out var chunk)) continue;

                foreach (var decalId  in chunk.Decals.Keys)
                {
                    OnDecalRemoved(gridId, decalId, gridComp, index, chunk);
                    gridComp.DecalIndex.Remove(decalId);
                }

                chunkCollection.Remove(index);
            }
        }
    }
}
