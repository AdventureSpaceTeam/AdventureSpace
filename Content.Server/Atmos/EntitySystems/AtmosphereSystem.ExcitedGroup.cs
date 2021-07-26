using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        private void ExcitedGroupAddTile(ExcitedGroup excitedGroup, TileAtmosphere tile)
        {
            DebugTools.Assert(!excitedGroup.Disposed, "Excited group is disposed!");
            DebugTools.Assert(tile.ExcitedGroup == null, "Tried to add a tile to an excited group when it's already in another one!");
            excitedGroup.Tiles.Add(tile);
            tile.ExcitedGroup = excitedGroup;
            ExcitedGroupResetCooldowns(excitedGroup);
        }

        private void ExcitedGroupRemoveTile(ExcitedGroup excitedGroup, TileAtmosphere tile)
        {
            DebugTools.Assert(!excitedGroup.Disposed, "Excited group is disposed!");
            DebugTools.Assert(tile.ExcitedGroup == excitedGroup, "Tried to remove a tile from an excited group it's not present in!");
            tile.ExcitedGroup = null;
            excitedGroup.Tiles.Remove(tile);
        }

        private void ExcitedGroupMerge(GridAtmosphereComponent gridAtmosphere, ExcitedGroup ourGroup, ExcitedGroup otherGroup)
        {
            DebugTools.Assert(!ourGroup.Disposed, "Excited group is disposed!");
            DebugTools.Assert(!otherGroup.Disposed, "Excited group is disposed!");
            DebugTools.Assert(gridAtmosphere.ExcitedGroups.Contains(ourGroup), "Grid Atmosphere does not contain Excited Group!");
            DebugTools.Assert(gridAtmosphere.ExcitedGroups.Contains(otherGroup), "Grid Atmosphere does not contain Excited Group!");
            var ourSize = ourGroup.Tiles.Count;
            var otherSize = otherGroup.Tiles.Count;

            ExcitedGroup winner;
            ExcitedGroup loser;

            if (ourSize > otherSize)
            {
                winner = ourGroup;
                loser = otherGroup;
            }
            else
            {
                winner = otherGroup;
                loser = ourGroup;
            }

            foreach (var tile in loser.Tiles)
            {
                tile.ExcitedGroup = winner;
                winner.Tiles.Add(tile);
            }

            loser.Tiles.Clear();
            ExcitedGroupDispose(gridAtmosphere, loser);
            ExcitedGroupResetCooldowns(winner);
        }

        private void ExcitedGroupResetCooldowns(ExcitedGroup excitedGroup)
        {
            DebugTools.Assert(!excitedGroup.Disposed, "Excited group is disposed!");
            excitedGroup.BreakdownCooldown = 0;
            excitedGroup.DismantleCooldown = 0;
        }

        private void ExcitedGroupSelfBreakdown(GridAtmosphereComponent gridAtmosphere, ExcitedGroup excitedGroup)
        {
            DebugTools.Assert(!excitedGroup.Disposed, "Excited group is disposed!");
            DebugTools.Assert(gridAtmosphere.ExcitedGroups.Contains(excitedGroup), "Grid Atmosphere does not contain Excited Group!");
            var combined = new GasMixture(Atmospherics.CellVolume);

            var tileSize = excitedGroup.Tiles.Count;

            if (excitedGroup.Disposed) return;

            if (tileSize == 0)
            {
                ExcitedGroupDispose(gridAtmosphere, excitedGroup);
                return;
            }

            foreach (var tile in excitedGroup.Tiles)
            {
                if (tile?.Air == null)
                    continue;

                Merge(combined, tile.Air);

                if (!ExcitedGroupsSpaceIsAllConsuming || !tile.Air.Immutable)
                    continue;

                combined.Clear();
                break;
            }

            combined.Multiply(1 / (float)tileSize);

            foreach (var tile in excitedGroup.Tiles)
            {
                if (tile?.Air == null) continue;
                tile.Air.CopyFromMutable(combined);
                InvalidateVisuals(tile.GridIndex, tile.GridIndices);
            }

            excitedGroup.BreakdownCooldown = 0;
        }

        private void ExcitedGroupDismantle(GridAtmosphereComponent gridAtmosphere, ExcitedGroup excitedGroup, bool unexcite = true)
        {
            foreach (var tile in excitedGroup.Tiles)
            {
                tile.ExcitedGroup = null;

                if (!unexcite)
                    continue;

                RemoveActiveTile(gridAtmosphere, tile);
            }

            excitedGroup.Tiles.Clear();
        }

        private void ExcitedGroupDispose(GridAtmosphereComponent gridAtmosphere, ExcitedGroup excitedGroup)
        {
            if (excitedGroup.Disposed)
                return;

            DebugTools.Assert(gridAtmosphere.ExcitedGroups.Contains(excitedGroup), "Grid Atmosphere does not contain Excited Group!");

            excitedGroup.Disposed = true;

            gridAtmosphere.ExcitedGroups.Remove(excitedGroup);
            ExcitedGroupDismantle(gridAtmosphere, excitedGroup, false);
        }
    }
}
