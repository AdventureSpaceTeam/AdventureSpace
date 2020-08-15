﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    public class TileAtmosphere : IGasMixtureHolder
    {
        [Robust.Shared.IoC.Dependency] private IRobustRandom _robustRandom = default!;
        [Robust.Shared.IoC.Dependency] private IEntityManager _entityManager = default!;
        [Robust.Shared.IoC.Dependency] private IMapManager _mapManager = default!;


        private static readonly TileAtmosphereComparer _comparer = new TileAtmosphereComparer();

        [ViewVariables]
        private int _archivedCycle = 0;

        [ViewVariables]
        private int _currentCycle = 0;

        [ViewVariables]
        private static GasTileOverlaySystem _gasTileOverlaySystem;

        [ViewVariables]
        private float _temperature = Atmospherics.T20C;

        [ViewVariables]
        private float _temperatureArchived = Atmospherics.T20C;

        // I know this being static is evil, but I seriously can't come up with a better solution to sound spam.
        private static int _soundCooldown = 0;

        [ViewVariables]
        public TileAtmosphere PressureSpecificTarget { get; set; } = null;

        [ViewVariables]
        public float PressureDifference { get; set; } = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public float HeatCapacity { get; set; } = 1f;

        [ViewVariables]
        public float ThermalConductivity => Tile?.Tile.GetContentTileDefinition().ThermalConductivity ?? 0.05f;

        [ViewVariables]
        public bool Excited { get; set; } = false;

        [ViewVariables]
        private GridAtmosphereComponent _gridAtmosphereComponent;

        [ViewVariables]
        private readonly Dictionary<Direction, TileAtmosphere> _adjacentTiles = new Dictionary<Direction, TileAtmosphere>();

        [ViewVariables]
        private TileAtmosInfo _tileAtmosInfo;

        [ViewVariables]
        public Hotspot Hotspot;

        private Direction _pressureDirection;

        [ViewVariables]
        public GridId GridIndex { get; }

        [ViewVariables]
        public TileRef? Tile => GridIndices.GetTileRef(GridIndex);

        [ViewVariables]
        public MapIndices GridIndices { get; }

        [ViewVariables]
        public ExcitedGroup ExcitedGroup { get; set; }

        [ViewVariables]
        public GasMixture Air { get; set; }

        [ViewVariables]
        public bool BlocksAir => _gridAtmosphereComponent.IsAirBlocked(GridIndices);

        public TileAtmosphere(GridAtmosphereComponent atmosphereComponent, GridId gridIndex, MapIndices gridIndices, GasMixture mixture = null)
        {
            IoCManager.InjectDependencies(this);
            _gridAtmosphereComponent = atmosphereComponent;
            GridIndex = gridIndex;
            GridIndices = gridIndices;
            Air = mixture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Archive(int fireCount)
        {
            Air?.Archive();
            _archivedCycle = fireCount;
            _temperatureArchived = _temperature;
        }

        public void HotspotExpose(float exposedTemperature, float exposedVolume, bool soh = false)
        {
            if (Air == null)
                return;

            var oxygen = Air.GetMoles(Gas.Oxygen);

            if (oxygen < 0.5f)
                return;

            var phoron = Air.GetMoles(Gas.Phoron);
            var tritium = Air.GetMoles(Gas.Tritium);

            if (Hotspot.Valid)
            {
                if (soh)
                {
                    if (phoron > 0.5f || tritium > 0.5f)
                    {
                        if (Hotspot.Temperature < exposedTemperature)
                            Hotspot.Temperature = exposedTemperature;
                        if (Hotspot.Volume < exposedVolume)
                            Hotspot.Volume = exposedVolume;
                    }
                }

                return;
            }

            if ((exposedTemperature > Atmospherics.PhoronMinimumBurnTemperature) && (phoron > 0.5f || tritium > 0.5f))
            {
                Hotspot = new Hotspot
                {
                    Volume = exposedVolume * 25f,
                    Temperature = exposedTemperature,
                    SkippedFirstProcess = _currentCycle > _gridAtmosphereComponent.UpdateCounter
                };

                Hotspot.Start();

                _gridAtmosphereComponent.AddActiveTile(this);
                _gridAtmosphereComponent.AddHotspotTile(this);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HighPressureMovements()
        {
            // TODO ATMOS finish this

            if(PressureDifference > 15)
            {
                if(_soundCooldown == 0)
                    EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Effects/space_wind.ogg",
                        GridIndices.ToGridCoordinates(_mapManager, GridIndex), AudioHelpers.WithVariation(0.125f).WithVolume(FloatMath.Clamp(PressureDifference / 10, 10, 100)));
            }


            foreach (var entity in _entityManager.GetEntitiesIntersecting(_mapManager.GetGrid(GridIndex).ParentMapId, Box2.UnitCentered.Translated(GridIndices)))
            {
                if (!entity.TryGetComponent(out ICollidableComponent physics)
                    ||  !entity.TryGetComponent(out MovedByPressureComponent pressure)
                    ||  ContainerHelpers.IsInContainer(entity))
                    continue;

                var pressureMovements = physics.EnsureController<HighPressureMovementController>();
                if (pressure.LastHighPressureMovementAirCycle < _gridAtmosphereComponent.UpdateCounter)
                {
                    pressureMovements.ExperiencePressureDifference(_gridAtmosphereComponent.UpdateCounter, PressureDifference, _pressureDirection, 0, PressureSpecificTarget?.GridIndices.ToGridCoordinates(_mapManager, GridIndex) ?? GridCoordinates.InvalidGrid);
                }

            }

            if (PressureDifference > 100)
            {
                // Do space wind graphics here!
            }

            _soundCooldown++;
            if (_soundCooldown > 75)
                _soundCooldown = 0;
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

                return a._tileAtmosInfo.MoleDelta.CompareTo(b._tileAtmosInfo.MoleDelta);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EqualizePressureInZone(int cycleNum)
        {
            if (Air == null || (_tileAtmosInfo.LastCycle >= cycleNum)) return; // Already done.

            ResetTileAtmosInfo();


            var startingMoles = Air.TotalMoles;
            var runAtmos = false;

            // We need to figure if this is necessary
            foreach (var (direction, other) in _adjacentTiles)
            {
                if (other?.Air == null) continue;
                var comparisonMoles = other.Air.TotalMoles;
                if (!(MathF.Abs(comparisonMoles - startingMoles) > Atmospherics.MinimumMolesDeltaToMove)) continue;
                runAtmos = true;
                break;
            }

            if (!runAtmos) // There's no need so we don't bother.
            {
                _tileAtmosInfo.LastCycle = cycleNum;
                return;
            }

            var queueCycle = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
            var totalMoles = 0f;
            var tiles = ArrayPool<TileAtmosphere>.Shared.Rent(Atmospherics.ZumosHardTileLimit);
            tiles[0] = this;
            _tileAtmosInfo.LastQueueCycle = queueCycle;
            var tileCount = 1;
            for (var i = 0; i < tileCount; i++)
            {
                if (i > Atmospherics.ZumosHardTileLimit) break;
                var exploring = tiles[i];

                if (i < Atmospherics.ZumosTileLimit)
                {
                    var tileMoles = exploring.Air.TotalMoles;
                    exploring._tileAtmosInfo.MoleDelta = tileMoles;
                    totalMoles += tileMoles;
                }

                foreach (var (_, adj) in exploring._adjacentTiles)
                {
                    if (adj?.Air == null) continue;
                    if(adj._tileAtmosInfo.LastQueueCycle == queueCycle) continue;
                    adj.ResetTileAtmosInfo();

                    adj._tileAtmosInfo.LastQueueCycle = queueCycle;
                    if(tileCount < Atmospherics.ZumosHardTileLimit)
                        tiles[tileCount++] = adj;
                    if (adj.Air.Immutable)
                    {
                        // Looks like someone opened an airlock to space!
                        ExplosivelyDepressurize(cycleNum);
                        return;
                    }
                }
            }

            if (tileCount > Atmospherics.ZumosTileLimit)
            {
                for (var i = Atmospherics.ZumosTileLimit; i < tileCount; i++)
                {
                    //We unmark them. We shouldn't be pushing/pulling gases to/from them.
                    var tile = tiles[i];
                    if (tile == null) continue;
                    tiles[i]._tileAtmosInfo.LastQueueCycle = 0;
                }

                tileCount = Atmospherics.ZumosTileLimit;
            }

            //tiles = tiles.AsSpan().Slice(0, tileCount).ToArray(); // According to my benchmarks, this is much slower.
            //Array.Resize(ref tiles, tileCount);

            var averageMoles = totalMoles / (tileCount);
            var giverTiles = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
            var takerTiles = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
            var giverTilesLength = 0;
            var takerTilesLength = 0;

            for (var i = 0; i < tileCount; i++)
            {
                var tile = tiles[i];
                tile._tileAtmosInfo.LastCycle = cycleNum;
                tile._tileAtmosInfo.MoleDelta -= averageMoles;
                if (tile._tileAtmosInfo.MoleDelta > 0)
                {
                    giverTiles[giverTilesLength++] = tile;
                }
                else
                {
                    takerTiles[takerTilesLength++] = tile;
                }
            }

            var logN = MathF.Log2(tileCount);

            // Optimization - try to spread gases using an O(nlogn) algorithm that has a chance of not working first to avoid O(n^2)
            if (giverTilesLength > logN && takerTilesLength > logN)
            {
                // Even if it fails, it will speed up the next part.
                Array.Sort(tiles, 0, tileCount, _comparer);

                for (var i = 0; i < tileCount; i++)
                {
                    var tile = tiles[i];
                    tile._tileAtmosInfo.FastDone = true;
                    if (!(tile._tileAtmosInfo.MoleDelta > 0)) continue;
                    Direction eligibleAdjBits = 0;
                    var amtEligibleAdj = 0;
                    foreach (var direction in Cardinal)
                    {
                        if (!tile._adjacentTiles.TryGetValue(direction, out var tile2)) continue;

                        // skip anything that isn't part of our current processing block. Original one didn't do this unfortunately, which probably cause some massive lag.
                        if (tile2._tileAtmosInfo.FastDone || tile2._tileAtmosInfo.LastQueueCycle != queueCycle)
                            continue;

                        eligibleAdjBits |= direction;
                        amtEligibleAdj++;
                    }

                    if (amtEligibleAdj <= 0)
                        continue; // Oof we've painted ourselves into a corner. Bad luck. Next part will handle this.
                    var molesToMove = tile._tileAtmosInfo.MoleDelta / amtEligibleAdj;
                    foreach (var direction in Cardinal)
                    {
                        if ((eligibleAdjBits & direction) == 0 ||
                            !tile._adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                        tile.AdjustEqMovement(direction, molesToMove);
                        tile._tileAtmosInfo.MoleDelta -= molesToMove;
                        tile2._tileAtmosInfo.MoleDelta += molesToMove;
                    }
                }

                giverTilesLength = 0;
                takerTilesLength = 0;

                for (var i = 0; i < tileCount; i++)
                {
                    var tile = tiles[i];
                    if (tile._tileAtmosInfo.MoleDelta > 0)
                    {
                        giverTiles[giverTilesLength++] = tile;
                    }
                    else
                    {
                        takerTiles[takerTilesLength++] = tile;
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
                        giver._tileAtmosInfo.CurrentTransferDirection = (Direction) (-1);
                        giver._tileAtmosInfo.CurrentTransferAmount = 0;
                        var queueCycleSlow = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
                        var queueLength = 0;
                        queue[queueLength++] = giver;
                        giver._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                        for (var i = 0; i < queueLength; i++)
                        {
                            if (giver._tileAtmosInfo.MoleDelta <= 0)
                                break; // We're done here now. Let's not do more work than needed.

                            var tile = queue[i];
                            foreach (var direction in Cardinal)
                            {
                                if (!tile._adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                                if (giver._tileAtmosInfo.MoleDelta <= 0)
                                    break; // We're done here now. Let's not do more work than needed.

                                if (tile2?._tileAtmosInfo == null || tile2._tileAtmosInfo.LastQueueCycle != queueCycle)
                                    continue;

                                if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                                queue[queueLength++] = tile2;
                                tile2._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                                tile2._tileAtmosInfo.CurrentTransferDirection = direction.GetOpposite();
                                tile2._tileAtmosInfo.CurrentTransferAmount = 0;
                                if (tile2._tileAtmosInfo.MoleDelta < 0)
                                {
                                    // This tile needs gas. Let's give it to 'em.
                                    if (-tile2._tileAtmosInfo.MoleDelta > giver._tileAtmosInfo.MoleDelta)
                                    {
                                        // We don't have enough gas!
                                        tile2._tileAtmosInfo.CurrentTransferAmount -= giver._tileAtmosInfo.MoleDelta;
                                        tile2._tileAtmosInfo.MoleDelta += giver._tileAtmosInfo.MoleDelta;
                                        giver._tileAtmosInfo.MoleDelta = 0;
                                    }
                                    else
                                    {
                                        // We have enough gas.
                                        tile2._tileAtmosInfo.CurrentTransferAmount += tile2._tileAtmosInfo.MoleDelta;
                                        giver._tileAtmosInfo.MoleDelta += tile2._tileAtmosInfo.MoleDelta;
                                        tile2._tileAtmosInfo.MoleDelta = 0;
                                    }
                                }
                            }
                        }

                        // Putting this loop here helps make it O(n^2) over O(n^3)
                        for (var i = queueLength - 1; i >= 0; i--)
                        {
                            var tile = queue[i];
                            if (tile._tileAtmosInfo.CurrentTransferAmount != 0 &&
                                tile._tileAtmosInfo.CurrentTransferDirection != (Direction) (-1))
                            {
                                tile.AdjustEqMovement(tile._tileAtmosInfo.CurrentTransferDirection,
                                    tile._tileAtmosInfo.CurrentTransferAmount);
                                if (tile._adjacentTiles.TryGetValue(tile._tileAtmosInfo.CurrentTransferDirection,
                                    out var adjacent))
                                    adjacent._tileAtmosInfo.CurrentTransferAmount +=
                                        tile._tileAtmosInfo.CurrentTransferAmount;
                                tile._tileAtmosInfo.CurrentTransferAmount = 0;
                            }
                        }
                    }

                    ArrayPool<TileAtmosphere>.Shared.Return(queue, true);
                }
                else
                {
                    var queue = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
                    for (var j = 0; j < takerTilesLength; j++)
                    {
                        var taker = takerTiles[j];
                        taker._tileAtmosInfo.CurrentTransferDirection = Direction.Invalid;
                        taker._tileAtmosInfo.CurrentTransferAmount = 0;
                        var queueCycleSlow = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
                        var queueLength = 0;
                        queue[queueLength++] = taker;
                        taker._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                        for (int i = 0; i < queueLength; i++)
                        {
                            if (taker._tileAtmosInfo.MoleDelta >= 0)
                                break; // We're done here now. Let's not do more work than needed.

                            var tile = queue[i];
                            foreach (var direction in Cardinal)
                            {
                                if (!tile._adjacentTiles.ContainsKey(direction)) continue;
                                var tile2 = tile._adjacentTiles[direction];

                                if (taker._tileAtmosInfo.MoleDelta >= 0)
                                    break; // We're done here now. Let's not do more work than needed.

                                if (tile2?._tileAtmosInfo == null || tile2._tileAtmosInfo.LastQueueCycle != queueCycle)
                                    continue;
                                if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                                queue[queueLength++] = tile2;
                                tile2._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                                tile2._tileAtmosInfo.CurrentTransferDirection = direction.GetOpposite();
                                tile2._tileAtmosInfo.CurrentTransferAmount = 0;

                                if (tile2._tileAtmosInfo.MoleDelta > 0)
                                {
                                    // This tile has gas we can suck, so let's
                                    if (tile2._tileAtmosInfo.MoleDelta > -taker._tileAtmosInfo.MoleDelta)
                                    {
                                        // They have enough gas
                                        tile2._tileAtmosInfo.CurrentTransferAmount -= taker._tileAtmosInfo.MoleDelta;
                                        tile2._tileAtmosInfo.MoleDelta += taker._tileAtmosInfo.MoleDelta;
                                        taker._tileAtmosInfo.MoleDelta = 0;
                                    }
                                    else
                                    {
                                        // They don't have enough gas!
                                        tile2._tileAtmosInfo.CurrentTransferAmount += tile2._tileAtmosInfo.MoleDelta;
                                        taker._tileAtmosInfo.MoleDelta += tile2._tileAtmosInfo.MoleDelta;
                                        tile2._tileAtmosInfo.MoleDelta = 0;
                                    }
                                }
                            }
                        }

                        for (var i = queueLength - 1; i >= 0; i--)
                        {
                            var tile = queue[i];
                            if (tile._tileAtmosInfo.CurrentTransferAmount == 0 || tile._tileAtmosInfo.CurrentTransferDirection == Direction.Invalid)
                                continue;

                            tile.AdjustEqMovement(tile._tileAtmosInfo.CurrentTransferDirection, tile._tileAtmosInfo.CurrentTransferAmount);

                            if (tile._adjacentTiles.TryGetValue(tile._tileAtmosInfo.CurrentTransferDirection, out var adjacent))
                            {
                                adjacent._tileAtmosInfo.CurrentTransferAmount += tile._tileAtmosInfo.CurrentTransferAmount;
                                tile._tileAtmosInfo.CurrentTransferAmount = 0;
                            }
                        }
                    }

                    ArrayPool<TileAtmosphere>.Shared.Return(queue, true);
                }

                for (var i = 0; i < tileCount; i++)
                {
                    var tile = tiles[i];
                    tile.FinalizeEq();
                }

                for (var i = 0; i < tileCount; i++)
                {
                    var tile = tiles[i];
                    foreach (var direction in Cardinal)
                    {
                        if (!tile._adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                        if (tile2?.Air?.Compare(Air) == GasMixture.GasCompareResult.NoExchange) continue;
                        _gridAtmosphereComponent.AddActiveTile(tile2);
                        break;
                    }
                }

                ArrayPool<TileAtmosphere>.Shared.Return(tiles, true);
                ArrayPool<TileAtmosphere>.Shared.Return(giverTiles, true);
                ArrayPool<TileAtmosphere>.Shared.Return(takerTiles, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinalizeEq()
        {
            var transferDirections = new Dictionary<Direction, float>();
            var hasTransferDirs = false;
            foreach (var direction in Cardinal)
            {
                var amount = _tileAtmosInfo[direction];
                transferDirections[direction] = amount;
                if (amount == 0) continue;
                _tileAtmosInfo[direction] = 0;
                hasTransferDirs = true;
            }

            if (!hasTransferDirs) return;

            foreach (var direction in Cardinal)
            {
                var amount = transferDirections[direction];
                if (!_adjacentTiles.TryGetValue(direction, out var tile) || tile.Air == null) continue;
                if (amount > 0)
                {
                    // Prevent infinite recursion.
                    tile._tileAtmosInfo[direction.GetOpposite()] = 0;

                    if (Air.TotalMoles < amount)
                        FinalizeEqNeighbors();

                    tile.Air.Merge(Air.Remove(amount));
                    UpdateVisuals();
                    tile.UpdateVisuals();
                    ConsiderPressureDifference(tile, amount);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinalizeEqNeighbors()
        {
            foreach (var direction in Cardinal)
            {
                var amount = _tileAtmosInfo[direction];
                if(amount < 0 && _adjacentTiles.TryGetValue(direction, out var adjacent))
                    adjacent.FinalizeEq();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConsiderPressureDifference(TileAtmosphere tile, float difference)
        {
            _gridAtmosphereComponent.AddHighPressureDelta(this);
            if (difference > PressureDifference)
            {
                PressureDifference = difference;
                _pressureDirection = ((Vector2i) (GridIndices - tile.GridIndices)).GetCardinalDir();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdjustEqMovement(Direction direction, float molesToMove)
        {
            _tileAtmosInfo[direction] += molesToMove;
            if(direction != Direction.Invalid && _adjacentTiles.TryGetValue(direction, out var adj))
                adj._tileAtmosInfo[direction.GetOpposite()] -= molesToMove;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetTileAtmosInfo()
        {
            _tileAtmosInfo = new TileAtmosInfo {CurrentTransferDirection = Direction.Invalid};
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessCell(int fireCount)
        {
            // Can't process a tile without air
            if (Air == null)
            {
                _gridAtmosphereComponent.RemoveActiveTile(this);
                return;
            }

            if (_archivedCycle < fireCount)
                Archive(fireCount);

            _currentCycle = fireCount;
            var adjacentTileLength = 0;
            foreach (var (_, enemyTile) in _adjacentTiles)
            {
                // If the tile is null or has no air, we don't do anything
                if(enemyTile?.Air == null) continue;
                adjacentTileLength++;
                if (fireCount <= enemyTile._currentCycle) continue;
                enemyTile.Archive(fireCount);

                var shouldShareAir = false;

                if (ExcitedGroup != null && enemyTile.ExcitedGroup != null)
                {
                    if (ExcitedGroup != enemyTile.ExcitedGroup)
                    {
                        ExcitedGroup.MergeGroups(enemyTile.ExcitedGroup);
                    }

                    shouldShareAir = true;
                } else if (Air.Compare(enemyTile.Air) != GasMixture.GasCompareResult.NoExchange)
                {
                    if (!enemyTile.Excited)
                    {
                        _gridAtmosphereComponent.AddActiveTile(enemyTile);
                    }

                    var excitedGroup = ExcitedGroup;
                    excitedGroup ??= enemyTile.ExcitedGroup;

                    if (excitedGroup == null)
                    {
                        excitedGroup = new ExcitedGroup();
                        excitedGroup.Initialize(_gridAtmosphereComponent);
                    }

                    if (ExcitedGroup == null)
                        excitedGroup.AddTile(this);

                    if(enemyTile.ExcitedGroup == null)
                        excitedGroup.AddTile(enemyTile);

                    shouldShareAir = true;
                }

                if (shouldShareAir)
                {
                    var difference = Air.Share(enemyTile.Air, adjacentTileLength);

                    // Space wind!
                    if (difference > 0)
                    {
                        ConsiderPressureDifference(enemyTile, difference);
                    }
                    else
                    {
                        enemyTile.ConsiderPressureDifference(this, -difference);
                    }

                    LastShareCheck();
                }
            }

            React();
            UpdateVisuals();

            if((!(Air.Temperature > Atmospherics.MinimumTemperatureStartSuperConduction && ConsiderSuperconductivity(true))) && ExcitedGroup == null)
                _gridAtmosphereComponent.RemoveActiveTile(this);
        }

        public void ProcessHotspot()
        {
            if (!Hotspot.Valid)
            {
                _gridAtmosphereComponent.RemoveHotspotTile(this);
                return;
            }

            if (!Hotspot.SkippedFirstProcess)
            {
                Hotspot.SkippedFirstProcess = true;
                return;
            }

            ExcitedGroup?.ResetCooldowns();

            if ((Hotspot.Temperature < Atmospherics.FireMinimumTemperatureToExist) || (Hotspot.Volume <= 1f)
                || Air == null || Air.Gases[(int)Gas.Oxygen] < 0.5f || (Air.Gases[(int)Gas.Phoron] < 0.5f && Air.GetMoles(Gas.Tritium) < 0.5f))
            {
                Hotspot = new Hotspot();
                UpdateVisuals();
                return;
            }

            PerformHotspotExposure();

            if (Hotspot.Bypassing)
            {
                Hotspot.State = 3;
                _gridAtmosphereComponent.BurnTile(GridIndices);

                if (Air.Temperature > Atmospherics.FireMinimumTemperatureToSpread)
                {
                    var radiatedTemperature = Air.Temperature * Atmospherics.FireSpreadRadiosityScale;
                    foreach (var (_, tile) in _adjacentTiles)
                    {
                        if(!tile.Hotspot.Valid)
                            tile.HotspotExpose(radiatedTemperature, Atmospherics.CellVolume/4);
                    }
                }
            }
            else
            {
                Hotspot.State = Hotspot.Volume > Atmospherics.CellVolume * 0.4f ? 2 : 1;
            }

            if (Hotspot.Temperature > MaxFireTemperatureSustained)
                MaxFireTemperatureSustained = Hotspot.Temperature;

            // TODO ATMOS Maybe destroy location here?
        }

        public float MaxFireTemperatureSustained { get; private set; }

        private void PerformHotspotExposure()
        {
            if (Air == null || !Hotspot.Valid) return;

            Hotspot.Bypassing = Hotspot.SkippedFirstProcess && (Hotspot.Volume > Atmospherics.CellVolume*0.95);

            if (Hotspot.Bypassing)
            {
                Hotspot.Volume = Air.ReactionResults[GasReaction.Fire] * Atmospherics.FireGrowthRate;
                Hotspot.Temperature = Air.Temperature;
            }
            else
            {
                var affected = Air.RemoveRatio(Hotspot.Volume / Air.Volume);
                if (affected != null)
                {
                    affected.Temperature = Hotspot.Temperature;
                    affected.React(this);
                    Hotspot.Temperature = affected.Temperature;
                    Hotspot.Volume = affected.ReactionResults[GasReaction.Fire] * Atmospherics.FireGrowthRate;
                    AssumeAir(affected);
                }
            }

            // TODO ATMOS Let all entities in this tile know about the fire?
        }

        private bool ConsiderSuperconductivity()
        {
            if (ThermalConductivity == 0f)
                return false;

            _gridAtmosphereComponent.AddSuperconductivityTile(this);
            return true;
        }

        private bool ConsiderSuperconductivity(bool starting)
        {
            if (Air.Temperature < (starting
                ? Atmospherics.MinimumTemperatureStartSuperConduction
                : Atmospherics.MinimumTemperatureForSuperconduction))
                return false;

            return !(Air.HeatCapacity < Atmospherics.MCellWithRatio) && ConsiderSuperconductivity();
        }

        public void Superconduct()
        {
            var directions = ConductivityDirections();
            var adjacentTiles = _gridAtmosphereComponent.GetAdjacentTiles(GridIndices, true);

            if (directions.Length > 0)
            {
                foreach (var direction in directions)
                {
                    if (!adjacentTiles.TryGetValue(direction, out var adjacent)) continue;

                    if (adjacent.ThermalConductivity == 0f)
                        continue;

                    if(adjacent._archivedCycle < _gridAtmosphereComponent.UpdateCounter)
                        adjacent.Archive(_gridAtmosphereComponent.UpdateCounter);

                    adjacent.NeighborConductWithSource(this);

                    adjacent.ConsiderSuperconductivity();
                }
            }

            RadiateToSpace();

            FinishSuperconduction();
        }

        private void FinishSuperconduction()
        {
            // Conduct with air on my tile if I have it
            if (!BlocksAir)
            {
                _temperature = Air.TemperatureShare(ThermalConductivity, _temperature, HeatCapacity);
            }

            FinishSuperconduction(BlocksAir ? _temperature : Air.Temperature);
        }

        private void FinishSuperconduction(float temperature)
        {
            // Make sure it's still hot enough to continue conducting.
            if (temperature < Atmospherics.MinimumTemperatureForSuperconduction)
            {
                _gridAtmosphereComponent.RemoveSuperconductivityTile(this);
            }
        }

        private void NeighborConductWithSource(TileAtmosphere other)
        {
            if (BlocksAir)
            {
                if (!other.BlocksAir)
                {
                    other.TemperatureShareOpenToSolid(this);
                }
                else
                {
                    other.TemperatureShareMutualSolid(this, ThermalConductivity);
                }

                TemperatureExpose(null, _temperature, _gridAtmosphereComponent.GetVolumeForCells(1));
                return;
            }

            if (!other.BlocksAir)
            {
                other.Air.TemperatureShare(Air, Atmospherics.WindowHeatTransferCoefficient);
            }
            else
            {
                TemperatureShareOpenToSolid(other);
            }

            _gridAtmosphereComponent.AddActiveTile(this);
        }

        private void TemperatureShareOpenToSolid(TileAtmosphere other)
        {
            other._temperature =
                Air.TemperatureShare(other.ThermalConductivity, other._temperature, other.HeatCapacity);
        }

        private void TemperatureShareMutualSolid(TileAtmosphere other, float conductionCoefficient)
        {
            var deltaTemperature = (_temperatureArchived - other._temperatureArchived);
            if (MathF.Abs(deltaTemperature) > Atmospherics.MinimumTemperatureDeltaToConsider
                && HeatCapacity != 0f && other.HeatCapacity != 0f)
            {
                var heat = conductionCoefficient * deltaTemperature *
                           (HeatCapacity * other.HeatCapacity / (HeatCapacity + other.HeatCapacity));

                _temperature -= heat / HeatCapacity;
                other._temperature += heat / other.HeatCapacity;
            }
        }

        public void RadiateToSpace()
        {
            // Considering 0ºC as the break even point for radiation in and out.
            if (_temperature > Atmospherics.T0C)
            {
                // Hardcoded space temperature.
                var deltaTemperature = (_temperatureArchived - Atmospherics.TCMB);
                if ((HeatCapacity > 0) && (MathF.Abs(deltaTemperature) > Atmospherics.MinimumTemperatureDeltaToConsider))
                {
                    var heat = ThermalConductivity * deltaTemperature * (HeatCapacity *
                        Atmospherics.HeatCapacityVacuum / (HeatCapacity + Atmospherics.HeatCapacityVacuum));

                    _temperature -= heat;
                }
            }
        }

        public Direction[] ConductivityDirections()
        {
            if(BlocksAir)
            {
                if(_archivedCycle < _gridAtmosphereComponent.UpdateCounter)
                    Archive(_gridAtmosphereComponent.UpdateCounter);
                return Cardinal;
            }

            // TODO ATMOS check if this is correct
            return Cardinal;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExplosivelyDepressurize(int cycleNum)
        {
            if (Air == null) return;
            var totalGasesRemoved = 0f;
            var queueCycle = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
            var tiles = new List<TileAtmosphere>();
            var spaceTiles = new List<TileAtmosphere>();
            tiles.Add(this);

            ResetTileAtmosInfo();
            _tileAtmosInfo.LastQueueCycle = queueCycle;

            var tileCount = 1;
            for (var i = 0; i < tileCount; i++)
            {
                var tile = tiles[i];
                tile._tileAtmosInfo.LastCycle = cycleNum;
                tile._tileAtmosInfo.CurrentTransferDirection = Direction.Invalid;
                if (tile.Air.Immutable)
                {
                    spaceTiles.Add(tile);
                    tile.PressureSpecificTarget = tile;
                }
                else
                {
                    if (i > Atmospherics.ZumosHardTileLimit) continue;
                    foreach (var direction in Cardinal)
                    {
                        if (!_adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                        if (tile2?.Air == null) continue;
                        if (tile2._tileAtmosInfo.LastQueueCycle == queueCycle) continue;
                        tile.ConsiderFirelocks(tile2);
                        if (tile._adjacentTiles[direction]?.Air != null)
                        {
                            tile2.ResetTileAtmosInfo();
                            tile2._tileAtmosInfo.LastQueueCycle = queueCycle;
                            tiles.Add(tile2);
                            tileCount++;
                        }
                    }
                }
            }

            var queueCycleSlow = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
            var progressionOrder = new List<TileAtmosphere>();
            foreach (var tile in spaceTiles)
            {
                progressionOrder.Add(tile);
                tile._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                tile._tileAtmosInfo.CurrentTransferDirection = Direction.Invalid;
            }

            var progressionCount = progressionOrder.Count;
            for (int i = 0; i < progressionCount; i++)
            {
                var tile = progressionOrder[i];
                foreach (var direction in Cardinal)
                {
                    if (!_adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                    if (tile2?._tileAtmosInfo.LastQueueCycle != queueCycle) continue;
                    if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                    if(tile2.Air.Immutable) continue;
                    tile2._tileAtmosInfo.CurrentTransferDirection = direction.GetOpposite();
                    tile2._tileAtmosInfo.CurrentTransferAmount = 0;
                    tile2.PressureSpecificTarget = tile.PressureSpecificTarget;
                    tile2._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                    progressionOrder.Add(tile2);
                    progressionCount++;
                }
            }

            for (int i = 0; i < progressionCount; i++)
            {
                var tile = progressionOrder[i];
                if (tile._tileAtmosInfo.CurrentTransferDirection == Direction.Invalid) continue;
                var hpdLength = _gridAtmosphereComponent.HighPressureDeltaCount;
                var inHdp = _gridAtmosphereComponent.HasHighPressureDelta(tile);
                if(!inHdp)
                    _gridAtmosphereComponent.AddHighPressureDelta(tile);
                if (!tile._adjacentTiles.TryGetValue(tile._tileAtmosInfo.CurrentTransferDirection, out var tile2) || tile2.Air == null) continue;
                var sum = tile2.Air.TotalMoles;
                totalGasesRemoved += sum;
                tile._tileAtmosInfo.CurrentTransferAmount += sum;
                tile2._tileAtmosInfo.CurrentTransferAmount += tile._tileAtmosInfo.CurrentTransferAmount;
                tile.PressureDifference = tile._tileAtmosInfo.CurrentTransferAmount;
                tile._pressureDirection = tile._tileAtmosInfo.CurrentTransferDirection;
                if (tile2._tileAtmosInfo.CurrentTransferDirection == Direction.Invalid)
                {
                    tile2.PressureDifference = tile2._tileAtmosInfo.CurrentTransferAmount;
                    tile2._pressureDirection = tile._tileAtmosInfo.CurrentTransferDirection;
                }
                tile.Air.Clear();
                tile.UpdateVisuals();
                tile.HandleDecompressionFloorRip(sum);
            }
        }

        private void HandleDecompressionFloorRip(float sum)
        {
            if (sum > 20 && _robustRandom.Prob(FloatMath.Clamp(sum / 100, 0.005f, 0.5f)))
                _gridAtmosphereComponent.PryTile(GridIndices);
        }

        private void ConsiderFirelocks(TileAtmosphere other)
        {
            // TODO ATMOS firelocks!
            //throw new NotImplementedException();
        }

        private void React()
        {
            // TODO ATMOS I think this is enough? gotta make sure...
            Air?.React(this);
        }

        public bool AssumeAir(GasMixture giver)
        {
            if (giver == null || Air == null) return false;

            Air.Merge(giver);

            UpdateVisuals();

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateVisuals()
        {
            if (Air == null) return;

            _gasTileOverlaySystem ??= EntitySystem.Get<GasTileOverlaySystem>();
            _gasTileOverlaySystem.Invalidate(GridIndex, GridIndices);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAdjacent()
        {
            foreach (var direction in Cardinal)
            {
                if(!_gridAtmosphereComponent.IsAirBlocked(GridIndices.Offset(direction)))
                    _adjacentTiles[direction] = _gridAtmosphereComponent.GetTile(GridIndices.Offset(direction));
            }
        }

        public void UpdateAdjacent(Direction direction)
        {
            _adjacentTiles[direction] = _gridAtmosphereComponent.GetTile(GridIndices.Offset(direction));
        }

        private void LastShareCheck()
        {
            var lastShare = Air.LastShare;
            if (lastShare > Atmospherics.MinimumAirToSuspend)
            {
                ExcitedGroup.ResetCooldowns();
            } else if (lastShare > Atmospherics.MinimumMolesDeltaToMove)
            {
                ExcitedGroup.DismantleCooldown = 0;
            }
        }

        private static readonly Direction[] Cardinal =
            new Direction[]
            {
                Direction.North, Direction.East, Direction.South, Direction.West
            };

        public void TemperatureExpose(GasMixture mixture, float temperature, float cellVolume)
        {
            // TODO ATMOS do this
        }
    }
}
