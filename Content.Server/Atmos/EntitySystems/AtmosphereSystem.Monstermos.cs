#nullable disable warnings
#nullable enable annotations
using System;
using System.Buffers;
using System.Collections.Generic;
using Content.Server.Atmos.Components;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Atmos;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private readonly TileAtmosphereComparer _monstermosComparer = new();

        public void EqualizePressureInZone(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, int cycleNum)
        {
            if (tile.Air == null || (tile.MonstermosInfo.LastCycle >= cycleNum))
                return; // Already done.

            tile.MonstermosInfo = new MonstermosInfo();

            var startingMoles = tile.Air.TotalMoles;
            var runAtmos = false;

            // We need to figure if this is necessary
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!tile.AdjacentBits.IsFlagSet(direction)) continue;
                var other = tile.AdjacentTiles[i];
                if (other?.Air == null) continue;
                var comparisonMoles = other.Air.TotalMoles;
                if (!(MathF.Abs(comparisonMoles - startingMoles) > Atmospherics.MinimumMolesDeltaToMove)) continue;
                runAtmos = true;
                break;
            }

            if (!runAtmos) // There's no need so we don't bother.
            {
                tile.MonstermosInfo.LastCycle = cycleNum;
                return;
            }

            var queueCycle = ++gridAtmosphere.EqualizationQueueCycleControl;
            var totalMoles = 0f;
            var tiles = ArrayPool<TileAtmosphere>.Shared.Rent(Atmospherics.MonstermosHardTileLimit);
            tiles[0] = tile;
            tile.MonstermosInfo.LastQueueCycle = queueCycle;
            var tileCount = 1;
            for (var i = 0; i < tileCount; i++)
            {
                if (i > Atmospherics.MonstermosHardTileLimit) break;
                var exploring = tiles[i];

                if (i < Atmospherics.MonstermosTileLimit)
                {
                    var tileMoles = exploring.Air.TotalMoles;
                    exploring.MonstermosInfo.MoleDelta = tileMoles;
                    totalMoles += tileMoles;
                }

                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    if (!exploring.AdjacentBits.IsFlagSet(direction)) continue;
                    var adj = exploring.AdjacentTiles[j];
                    if (adj?.Air == null) continue;
                    if(adj.MonstermosInfo.LastQueueCycle == queueCycle) continue;
                    adj.MonstermosInfo = new MonstermosInfo {LastQueueCycle = queueCycle};

                    if(tileCount < Atmospherics.MonstermosHardTileLimit)
                        tiles[tileCount++] = adj;

                    if (adj.Air.Immutable)
                    {
                        // Looks like someone opened an airlock to space!

                        ExplosivelyDepressurize(mapGrid, gridAtmosphere, tile, cycleNum);
                        return;
                    }
                }
            }

            if (tileCount > Atmospherics.MonstermosTileLimit)
            {
                for (var i = Atmospherics.MonstermosTileLimit; i < tileCount; i++)
                {
                    //We unmark them. We shouldn't be pushing/pulling gases to/from them.
                    var otherTile = tiles[i];

                    if (otherTile == null)
                        continue;

                    tiles[i].MonstermosInfo.LastQueueCycle = 0;
                }

                tileCount = Atmospherics.MonstermosTileLimit;
            }

            var averageMoles = totalMoles / (tileCount);
            var giverTiles = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
            var takerTiles = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
            var giverTilesLength = 0;
            var takerTilesLength = 0;

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = tiles[i];
                otherTile.MonstermosInfo.LastCycle = cycleNum;
                otherTile.MonstermosInfo.MoleDelta -= averageMoles;
                if (otherTile.MonstermosInfo.MoleDelta > 0)
                {
                    giverTiles[giverTilesLength++] = otherTile;
                }
                else
                {
                    takerTiles[takerTilesLength++] = otherTile;
                }
            }

            var logN = MathF.Log2(tileCount);

            // Optimization - try to spread gases using an O(nlogn) algorithm that has a chance of not working first to avoid O(n^2)
            if (giverTilesLength > logN && takerTilesLength > logN)
            {
                // Even if it fails, it will speed up the next part.
                Array.Sort(tiles, 0, tileCount, _monstermosComparer);

                for (var i = 0; i < tileCount; i++)
                {
                    var otherTile = tiles[i];
                    otherTile.MonstermosInfo.FastDone = true;
                    if (!(otherTile.MonstermosInfo.MoleDelta > 0)) continue;
                    var eligibleDirections = AtmosDirection.Invalid;
                    var eligibleDirectionCount = 0;
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                        var tile2 = otherTile.AdjacentTiles[j];

                        // skip anything that isn't part of our current processing block.
                        if (tile2.MonstermosInfo.FastDone || tile2.MonstermosInfo.LastQueueCycle != queueCycle)
                            continue;

                        eligibleDirections |= direction;
                        eligibleDirectionCount++;
                    }

                    if (eligibleDirectionCount <= 0)
                        continue; // Oof we've painted ourselves into a corner. Bad luck. Next part will handle this.

                    var molesToMove = otherTile.MonstermosInfo.MoleDelta / eligibleDirectionCount;
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!eligibleDirections.IsFlagSet(direction)) continue;

                        AdjustEqMovement(otherTile, direction, molesToMove);
                        otherTile.MonstermosInfo.MoleDelta -= molesToMove;
                        otherTile.AdjacentTiles[j].MonstermosInfo.MoleDelta += molesToMove;
                    }
                }

                giverTilesLength = 0;
                takerTilesLength = 0;

                for (var i = 0; i < tileCount; i++)
                {
                    var otherTile = tiles[i];
                    if (otherTile.MonstermosInfo.MoleDelta > 0)
                    {
                        giverTiles[giverTilesLength++] = otherTile;
                    }
                    else
                    {
                        takerTiles[takerTilesLength++] = otherTile;
                    }
                }
            }

            // This is the part that can become O(n^2).
            if (giverTilesLength < takerTilesLength)
            {
                // as an optimization, we choose one of two methods based on which list is smaller. We really want to avoid O(n^2) if we can.
                var queue = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
                for (var j = 0; j < giverTilesLength; j++)
                {
                    var giver = giverTiles[j];
                    giver.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                    giver.MonstermosInfo.CurrentTransferAmount = 0;
                    var queueCycleSlow = ++gridAtmosphere.EqualizationQueueCycleControl;
                    var queueLength = 0;
                    queue[queueLength++] = giver;
                    giver.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                    for (var i = 0; i < queueLength; i++)
                    {
                        if (giver.MonstermosInfo.MoleDelta <= 0)
                            break; // We're done here now. Let's not do more work than needed.

                        var otherTile = queue[i];
                        for (var k = 0; k < Atmospherics.Directions; k++)
                        {
                            var direction = (AtmosDirection) (1 << k);
                            if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                            var otherTile2 = otherTile.AdjacentTiles[k];
                            if (giver.MonstermosInfo.MoleDelta <= 0) break; // We're done here now. Let's not do more work than needed.
                            if (otherTile2 == null || otherTile2.MonstermosInfo.LastQueueCycle != queueCycle) continue;
                            if (otherTile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow) continue;

                            queue[queueLength++] = otherTile2;
                            otherTile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                            otherTile2.MonstermosInfo.CurrentTransferDirection = direction.GetOpposite();
                            otherTile2.MonstermosInfo.CurrentTransferAmount = 0;
                            if (otherTile2.MonstermosInfo.MoleDelta < 0)
                            {
                                // This tile needs gas. Let's give it to 'em.
                                if (-otherTile2.MonstermosInfo.MoleDelta > giver.MonstermosInfo.MoleDelta)
                                {
                                    // We don't have enough gas!
                                    otherTile2.MonstermosInfo.CurrentTransferAmount -= giver.MonstermosInfo.MoleDelta;
                                    otherTile2.MonstermosInfo.MoleDelta += giver.MonstermosInfo.MoleDelta;
                                    giver.MonstermosInfo.MoleDelta = 0;
                                }
                                else
                                {
                                    // We have enough gas.
                                    otherTile2.MonstermosInfo.CurrentTransferAmount += otherTile2.MonstermosInfo.MoleDelta;
                                    giver.MonstermosInfo.MoleDelta += otherTile2.MonstermosInfo.MoleDelta;
                                    otherTile2.MonstermosInfo.MoleDelta = 0;
                                }
                            }
                        }
                    }

                    // Putting this loop here helps make it O(n^2) over O(n^3)
                    for (var i = queueLength - 1; i >= 0; i--)
                    {
                        var otherTile = queue[i];
                        if (otherTile.MonstermosInfo.CurrentTransferAmount != 0 && otherTile.MonstermosInfo.CurrentTransferDirection != AtmosDirection.Invalid)
                        {
                            AdjustEqMovement(otherTile, otherTile.MonstermosInfo.CurrentTransferDirection, otherTile.MonstermosInfo.CurrentTransferAmount);
                            otherTile.AdjacentTiles[otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex()]
                                .MonstermosInfo.CurrentTransferAmount += otherTile.MonstermosInfo.CurrentTransferAmount;
                            otherTile.MonstermosInfo.CurrentTransferAmount = 0;
                        }
                    }
                }

                ArrayPool<TileAtmosphere>.Shared.Return(queue);
            }
            else
            {
                var queue = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
                for (var j = 0; j < takerTilesLength; j++)
                {
                    var taker = takerTiles[j];
                    taker.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                    taker.MonstermosInfo.CurrentTransferAmount = 0;
                    var queueCycleSlow = ++gridAtmosphere.EqualizationQueueCycleControl;
                    var queueLength = 0;
                    queue[queueLength++] = taker;
                    taker.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                    for (var i = 0; i < queueLength; i++)
                    {
                        if (taker.MonstermosInfo.MoleDelta >= 0)
                            break; // We're done here now. Let's not do more work than needed.

                        var otherTile = queue[i];
                        for (var k = 0; k < Atmospherics.Directions; k++)
                        {
                            var direction = (AtmosDirection) (1 << k);
                            if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                            var otherTile2 = otherTile.AdjacentTiles[k];

                            if (taker.MonstermosInfo.MoleDelta >= 0) break; // We're done here now. Let's not do more work than needed.
                            if (otherTile2 == null || otherTile2.MonstermosInfo.LastQueueCycle != queueCycle) continue;
                            if (otherTile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                            queue[queueLength++] = otherTile2;
                            otherTile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                            otherTile2.MonstermosInfo.CurrentTransferDirection = direction.GetOpposite();
                            otherTile2.MonstermosInfo.CurrentTransferAmount = 0;

                            if (otherTile2.MonstermosInfo.MoleDelta > 0)
                            {
                                // This tile has gas we can suck, so let's
                                if (otherTile2.MonstermosInfo.MoleDelta > -taker.MonstermosInfo.MoleDelta)
                                {
                                    // They have enough gas
                                    otherTile2.MonstermosInfo.CurrentTransferAmount -= taker.MonstermosInfo.MoleDelta;
                                    otherTile2.MonstermosInfo.MoleDelta += taker.MonstermosInfo.MoleDelta;
                                    taker.MonstermosInfo.MoleDelta = 0;
                                }
                                else
                                {
                                    // They don't have enough gas!
                                    otherTile2.MonstermosInfo.CurrentTransferAmount += otherTile2.MonstermosInfo.MoleDelta;
                                    taker.MonstermosInfo.MoleDelta += otherTile2.MonstermosInfo.MoleDelta;
                                    otherTile2.MonstermosInfo.MoleDelta = 0;
                                }
                            }
                        }
                    }

                    for (var i = queueLength - 1; i >= 0; i--)
                    {
                        var otherTile = queue[i];
                        if (otherTile.MonstermosInfo.CurrentTransferAmount == 0 || otherTile.MonstermosInfo.CurrentTransferDirection == AtmosDirection.Invalid)
                            continue;

                        AdjustEqMovement(otherTile, otherTile.MonstermosInfo.CurrentTransferDirection, otherTile.MonstermosInfo.CurrentTransferAmount);

                        otherTile.AdjacentTiles[otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex()]
                            .MonstermosInfo.CurrentTransferAmount += otherTile.MonstermosInfo.CurrentTransferAmount;
                        otherTile.MonstermosInfo.CurrentTransferAmount = 0;
                    }
                }

                ArrayPool<TileAtmosphere>.Shared.Return(queue);
            }

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = tiles[i];
                FinalizeEq(gridAtmosphere, otherTile);
            }

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = tiles[i];
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                    var otherTile2 = otherTile.AdjacentTiles[j];
                    if (otherTile2?.Air?.Compare(tile.Air) == GasMixture.GasCompareResult.NoExchange) continue;
                    AddActiveTile(gridAtmosphere, otherTile2);
                    break;
                }
            }

            ArrayPool<TileAtmosphere>.Shared.Return(tiles);
            ArrayPool<TileAtmosphere>.Shared.Return(giverTiles);
            ArrayPool<TileAtmosphere>.Shared.Return(takerTiles);
        }

        public void ExplosivelyDepressurize(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, int cycleNum)
        {
            // Check if explosive depressurization is enabled and if the tile is valid.
            if (!MonstermosDepressurization || tile.Air == null)
                return;

            const int limit = Atmospherics.MonstermosHardTileLimit;

            var totalGasesRemoved = 0f;
            var queueCycle = ++gridAtmosphere.EqualizationQueueCycleControl;
            var tiles = ArrayPool<TileAtmosphere>.Shared.Rent(limit);
            var spaceTiles = ArrayPool<TileAtmosphere>.Shared.Rent(limit);

            var tileCount = 0;
            var spaceTileCount = 0;

            tiles[tileCount++] = tile;

            tile.MonstermosInfo = new MonstermosInfo {LastQueueCycle = queueCycle};

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = tiles[i];
                otherTile.MonstermosInfo.LastCycle = cycleNum;
                otherTile.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                if (otherTile.Air.Immutable)
                {
                    spaceTiles[spaceTileCount++] = otherTile;
                    otherTile.PressureSpecificTarget = otherTile;
                }
                else
                {
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                        var otherTile2 = otherTile.AdjacentTiles[j];
                        if (otherTile2.Air == null) continue;
                        if (otherTile2.MonstermosInfo.LastQueueCycle == queueCycle) continue;

                        ConsiderFirelocks(gridAtmosphere, otherTile, otherTile2);

                        // The firelocks might have closed on us.
                        if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                        otherTile2.MonstermosInfo = new MonstermosInfo {LastQueueCycle = queueCycle};
                        tiles[tileCount++] = otherTile2;
                    }
                }

                if (tileCount >= limit || spaceTileCount >= limit)
                    break;
            }

            var queueCycleSlow = ++gridAtmosphere.EqualizationQueueCycleControl;
            var progressionOrder = ArrayPool<TileAtmosphere>.Shared.Rent(limit * 2);
            var progressionCount = 0;

            for (var i = 0; i < spaceTileCount; i++)
            {
                var otherTile = spaceTiles[i];
                progressionOrder[progressionCount++] = otherTile;
                otherTile.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                otherTile.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
            }

            for (var i = 0; i < progressionCount; i++)
            {
                var otherTile = progressionOrder[i];
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    // TODO ATMOS This is a terrible hack that accounts for the mess that are space TileAtmospheres.
                    if (!otherTile.AdjacentBits.IsFlagSet(direction) && !otherTile.Air.Immutable) continue;
                    var tile2 = otherTile.AdjacentTiles[j];
                    if (tile2?.MonstermosInfo.LastQueueCycle != queueCycle) continue;
                    if (tile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                    if(tile2.Air?.Immutable ?? false) continue;
                    tile2.MonstermosInfo.CurrentTransferDirection = direction.GetOpposite();
                    tile2.MonstermosInfo.CurrentTransferAmount = 0;
                    tile2.PressureSpecificTarget = otherTile.PressureSpecificTarget;
                    tile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                    progressionOrder[progressionCount++] = tile2;
                }
            }

            for (var i = progressionCount - 1; i >= 0; i--)
            {
                var otherTile = progressionOrder[i];
                if (otherTile.MonstermosInfo.CurrentTransferDirection == AtmosDirection.Invalid) continue;
                gridAtmosphere.HighPressureDelta.Add(otherTile);
                AddActiveTile(gridAtmosphere, otherTile);
                var otherTile2 = otherTile.AdjacentTiles[otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex()];
                if (otherTile2?.Air == null) continue;
                var sum = otherTile2.Air.TotalMoles;
                totalGasesRemoved += sum;
                otherTile.MonstermosInfo.CurrentTransferAmount += sum;
                otherTile2.MonstermosInfo.CurrentTransferAmount += otherTile.MonstermosInfo.CurrentTransferAmount;
                otherTile.PressureDifference = otherTile.MonstermosInfo.CurrentTransferAmount;
                otherTile.PressureDirection = otherTile.MonstermosInfo.CurrentTransferDirection;

                if (otherTile2.MonstermosInfo.CurrentTransferDirection == AtmosDirection.Invalid)
                {
                    otherTile2.PressureDifference = otherTile2.MonstermosInfo.CurrentTransferAmount;
                    otherTile2.PressureDirection = otherTile.MonstermosInfo.CurrentTransferDirection;
                }

                otherTile.Air?.Clear();
                InvalidateVisuals(otherTile.GridIndex, otherTile.GridIndices);
                HandleDecompressionFloorRip(mapGrid, otherTile, sum);
            }

            ArrayPool<TileAtmosphere>.Shared.Return(tiles);
            ArrayPool<TileAtmosphere>.Shared.Return(spaceTiles);
            ArrayPool<TileAtmosphere>.Shared.Return(progressionOrder);
        }

        private void ConsiderFirelocks(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, TileAtmosphere other)
        {
            if (!_mapManager.TryGetGrid(tile.GridIndex, out var mapGrid))
                return;

            var reconsiderAdjacent = false;

            foreach (var entity in mapGrid.GetAnchoredEntities(tile.GridIndices))
            {
                if (!ComponentManager.TryGetComponent(entity, out FirelockComponent firelock))
                    continue;

                reconsiderAdjacent |= firelock.EmergencyPressureStop();
            }

            foreach (var entity in mapGrid.GetAnchoredEntities(other.GridIndices))
            {
                if (!ComponentManager.TryGetComponent(entity, out FirelockComponent firelock))
                    continue;

                reconsiderAdjacent |= firelock.EmergencyPressureStop();
            }

            if (!reconsiderAdjacent)
                return;

            UpdateAdjacent(mapGrid, gridAtmosphere, tile);
            UpdateAdjacent(mapGrid, gridAtmosphere, other);
            InvalidateVisuals(tile.GridIndex, tile.GridIndices);
            InvalidateVisuals(other.GridIndex, other.GridIndices);
        }

        public void FinalizeEq(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
        {
            Span<float> transferDirections = stackalloc float[Atmospherics.Directions];
            var hasTransferDirs = false;
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var amount = tile.MonstermosInfo[i];
                if (amount == 0) continue;
                transferDirections[i] = amount;
                tile.MonstermosInfo[i] = 0; // Set them to 0 to prevent infinite recursion.
                hasTransferDirs = true;
            }

            if (!hasTransferDirs) return;

            for(var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!tile.AdjacentBits.IsFlagSet(direction)) continue;
                var amount = transferDirections[i];
                var otherTile = tile.AdjacentTiles[i];
                if (otherTile?.Air == null) continue;
                if (amount > 0)
                {
                    if (tile.Air.TotalMoles < amount)
                        FinalizeEqNeighbors(gridAtmosphere, tile, transferDirections);

                    otherTile.MonstermosInfo[direction.GetOpposite()] = 0;
                    Merge(otherTile.Air, tile.Air.Remove(amount));
                    InvalidateVisuals(tile.GridIndex, tile.GridIndices);
                    InvalidateVisuals(otherTile.GridIndex, otherTile.GridIndices);
                    ConsiderPressureDifference(gridAtmosphere, tile, otherTile, amount);
                }
            }
        }

        private void FinalizeEqNeighbors(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, ReadOnlySpan<float> transferDirs)
        {
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                var amount = transferDirs[i];
                if(amount < 0 && tile.AdjacentBits.IsFlagSet(direction))
                    FinalizeEq(gridAtmosphere, tile.AdjacentTiles[i]);  // A bit of recursion if needed.
            }
        }

        private void AdjustEqMovement(TileAtmosphere tile, AtmosDirection direction, float amount)
        {
            tile.MonstermosInfo[direction] += amount;
            tile.AdjacentTiles[direction.ToIndex()].MonstermosInfo[direction.GetOpposite()] -= amount;
        }

        private void HandleDecompressionFloorRip(IMapGrid mapGrid, TileAtmosphere tile, float sum)
        {
            if (!MonstermosRipTiles)
                return;

            var chance = MathHelper.Clamp(sum / 500, 0.005f, 0.5f);

            if (sum > 20 && _robustRandom.Prob(chance))
                PryTile(mapGrid, tile.GridIndices);
        }

        private class TileAtmosphereComparer : IComparer<TileAtmosphere>
        {
            public int Compare(TileAtmosphere a, TileAtmosphere b)
            {
                if (a == null && b == null)
                    return 0;

                if (a == null)
                    return -1;

                if (b == null)
                    return 1;

                return a.MonstermosInfo.MoleDelta.CompareTo(b.MonstermosInfo.MoleDelta);
            }
        }
    }
}
