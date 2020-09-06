﻿using System.Collections.Generic;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public interface IGridAtmosphereComponent : IComponent, IEnumerable<TileAtmosphere>
    {
        /// <summary>
        ///     Number of times <see cref="Update"/> has been called.
        /// </summary>
        int UpdateCounter { get; }

        /// <summary>
        ///     Control variable for equalization.
        /// </summary>
        long EqualizationQueueCycleControl { get; set; }

        /// <summary>
        ///     Attemps to pry a tile.
        /// </summary>
        /// <param name="indices"></param>
        void PryTile(MapIndices indices);

        /// <summary>
        ///     Burns a tile.
        /// </summary>
        /// <param name="gridIndices"></param>
        void BurnTile(MapIndices gridIndices);

        /// <summary>
        ///     Invalidates a coordinate to be revalidated again.
        ///     Use this after changing a tile's gas contents, or when the tile becomes space, etc.
        /// </summary>
        /// <param name="indices"></param>
        void Invalidate(MapIndices indices);

        /// <summary>
        ///     Attempts to fix a sudden vacuum by creating gas.
        /// </summary>
        void FixVacuum(MapIndices indices);

        /// <summary>
        ///     Adds an active tile so it becomes processed every update until it becomes inactive.
        ///     Also makes the tile excited.
        /// </summary>
        /// <param name="tile"></param>
        void AddActiveTile(TileAtmosphere tile);

        /// <summary>
        ///     Removes an active tile and disposes of its <seealso cref="ExcitedGroup"/>.
        ///     Use with caution.
        /// </summary>
        /// <param name="tile"></param>
        void RemoveActiveTile(TileAtmosphere tile);

        /// <summary>
        ///     Marks a tile as having a hotspot so it can be processed.
        /// </summary>
        /// <param name="tile"></param>
        void AddHotspotTile(TileAtmosphere tile);

        /// <summary>
        ///     Removes a tile from the hotspot processing list.
        /// </summary>
        /// <param name="tile"></param>
        void RemoveHotspotTile(TileAtmosphere tile);

        /// <summary>
        ///     Marks a tile as superconductive so it can be processed.
        /// </summary>
        /// <param name="tile"></param>
        void AddSuperconductivityTile(TileAtmosphere tile);

        /// <summary>
        ///     Removes a tile from the superconductivity processing list.
        /// </summary>
        /// <param name="tile"></param>
        void RemoveSuperconductivityTile(TileAtmosphere tile);

        /// <summary>
        ///     Marks a tile has having high pressure differences that need to be equalized.
        /// </summary>
        /// <param name="tile"></param>
        void AddHighPressureDelta(TileAtmosphere tile);

        /// <summary>
        ///     Returns whether the tile in question is marked as having high pressure differences or not.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        bool HasHighPressureDelta(TileAtmosphere tile);

        /// <summary>
        ///     Adds a excited group to be processed.
        /// </summary>
        /// <param name="excitedGroup"></param>
        void AddExcitedGroup(ExcitedGroup excitedGroup);

        /// <summary>
        ///     Removes an excited group.
        /// </summary>
        /// <param name="excitedGroup"></param>
        void RemoveExcitedGroup(ExcitedGroup excitedGroup);

        /// <summary>
        ///     Returns a tile.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        TileAtmosphere GetTile(MapIndices indices, bool createSpace = true);

        /// <summary>
        ///     Returns a tile.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        TileAtmosphere GetTile(EntityCoordinates coordinates, bool createSpace = true);

        /// <summary>
        ///     Returns if the tile in question is air-blocked.
        ///     This could be due to a wall, an airlock, etc.
        ///     Also see AirtightComponent.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        bool IsAirBlocked(MapIndices indices);

        /// <summary>
        ///     Returns if the tile in question is space.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        bool IsSpace(MapIndices indices);

        /// <summary>
        ///     Returns the volume in liters for a number of cells/tiles.
        /// </summary>
        /// <param name="cellCount"></param>
        /// <returns></returns>
        float GetVolumeForCells(int cellCount);

        void Update(float frameTime);

        void AddPipeNet(IPipeNet pipeNet);

        void RemovePipeNet(IPipeNet pipeNet);

        void AddPipeNetDevice(PipeNetDeviceComponent pipeNetDevice);

        void RemovePipeNetDevice(PipeNetDeviceComponent pipeNetDevice);
    }
}
