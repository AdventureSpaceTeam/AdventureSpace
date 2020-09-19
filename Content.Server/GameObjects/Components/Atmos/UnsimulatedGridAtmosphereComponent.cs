﻿#nullable enable
using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    [ComponentReference(typeof(IGridAtmosphereComponent))]
    [ComponentReference(typeof(GridAtmosphereComponent))]
    [Serializable]
    public class UnsimulatedGridAtmosphereComponent : GridAtmosphereComponent, IGridAtmosphereComponent
    {
        public override string Name => "UnsimulatedGridAtmosphere";

        public override void PryTile(MapIndices indices) { }

        public override void RepopulateTiles()
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;

            foreach (var tile in mapGrid.Grid.GetAllTiles())
            {
                if(!Tiles.ContainsKey(tile.GridIndices))
                    Tiles.Add(tile.GridIndices, new TileAtmosphere(this, tile.GridIndex, tile.GridIndices, new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C}));
            }
        }

        public override void Invalidate(MapIndices indices) { }

        protected override void Revalidate() { }

        public override void FixVacuum(MapIndices indices) { }

        public override void AddActiveTile(TileAtmosphere? tile) { }

        public override void RemoveActiveTile(TileAtmosphere? tile) { }

        public override void AddHotspotTile(TileAtmosphere? tile) { }

        public override void RemoveHotspotTile(TileAtmosphere? tile) { }

        public override void AddSuperconductivityTile(TileAtmosphere? tile) { }

        public override void RemoveSuperconductivityTile(TileAtmosphere? tile) { }

        public override void AddHighPressureDelta(TileAtmosphere? tile) { }

        public override bool HasHighPressureDelta(TileAtmosphere tile)
        {
            return false;
        }

        public override void AddExcitedGroup(ExcitedGroup excitedGroup) { }

        public override void RemoveExcitedGroup(ExcitedGroup excitedGroup) { }

        public override void AddPipeNet(IPipeNet pipeNet) { }

        public override void RemovePipeNet(IPipeNet pipeNet) { }

        public override void AddPipeNetDevice(PipeNetDeviceComponent pipeNetDevice) { }

        public override void RemovePipeNetDevice(PipeNetDeviceComponent pipeNetDevice) { }

        public override void Update(float frameTime) { }

        public override bool ProcessTileEqualize(bool resumed = false)
        {
            return false;
        }

        public override bool ProcessActiveTiles(bool resumed = false)
        {
            return false;
        }

        public override bool ProcessExcitedGroups(bool resumed = false)
        {
            return false;
        }

        public override bool ProcessHighPressureDelta(bool resumed = false)
        {
            return false;
        }

        protected override bool ProcessHotspots(bool resumed = false)
        {
            return false;
        }

        protected override bool ProcessSuperconductivity(bool resumed = false)
        {
            return false;
        }

        protected override bool ProcessPipeNets(bool resumed = false)
        {
            return false;
        }

        protected override bool ProcessPipeNetDevices(bool resumed = false)
        {
            return false;
        }
    }
}
