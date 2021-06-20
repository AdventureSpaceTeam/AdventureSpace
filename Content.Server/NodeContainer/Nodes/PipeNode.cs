#nullable enable
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.Interfaces;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Connects with other <see cref="PipeNode"/>s whose <see cref="PipeDirection"/>
    ///     correctly correspond.
    /// </summary>
    [DataDefinition]
    public class PipeNode : Node, IGasMixtureHolder, IRotatableNode
    {
        private PipeDirection _connectedDirections;

        /// <summary>
        ///     The directions in which this pipe can connect to other pipes around it.
        ///     Used to check if this pipe can connect to another pipe in a given direction.
        /// </summary>
        [ViewVariables]
        [DataField("pipeDirection")]
        public PipeDirection PipeDirection { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("connectToContainedEntities")]
        public bool ConnectToContainedEntities { get; set; } = false;

        /// <summary>
        ///     The directions in which this node is connected to other nodes.
        ///     Used by <see cref="PipeVisualState"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public PipeDirection ConnectedDirections
        {
            get => _connectedDirections;
            private set
            {
                _connectedDirections = value;
                UpdateAppearance();
            }
        }

        /// <summary>
        ///     Whether this node can connect to others or not.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ConnectionsEnabled
        {
            get => _connectionsEnabled;
            set
            {
                _connectionsEnabled = value;
                RefreshNodeGroup();
            }
        }

        [DataField("rotationsEnabled")]
        public bool RotationsEnabled { get; set; } = true;

        /// <summary>
        ///     The <see cref="IPipeNet"/> this pipe is a part of. Set to <see cref="PipeNet.NullNet"/> when not in an <see cref="IPipeNet"/>.
        /// </summary>
        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        [DataField("connectionsEnabled")]
        private bool _connectionsEnabled = true;

        /// <summary>
        ///     Whether to ignore the pipenet and return the environment's air.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("environmentalAir")]
        public bool EnvironmentalAir { get; set; } = false;

        /// <summary>
        ///     The gases in this pipe.
        /// </summary>
        [ViewVariables]
        public GasMixture Air
        {
            get => !EnvironmentalAir ? _pipeNet.Air : Owner.Transform.Coordinates.GetTileAir() ?? GasMixture.SpaceGas;
            set => _pipeNet.Air = value;
        }

        public void AssumeAir(GasMixture giver)
        {
            if (EnvironmentalAir)
            {
                var tileAtmosphere = Owner.Transform.Coordinates.GetTileAtmosphere();
                tileAtmosphere?.AssumeAir(giver);
                return;
            }

            _pipeNet.Air.Merge(giver);
        }

        [ViewVariables]
        [DataField("volume")]
        public float Volume { get; set; } = DefaultVolume;

        private const float DefaultVolume = 200f;

        public override void OnContainerStartup()
        {
            base.OnContainerStartup();
            OnConnectedDirectionsNeedsUpdating();
        }

        public override void OnContainerShutdown()
        {
            base.OnContainerShutdown();
            UpdateAdjacentConnectedDirections();
        }

        public void JoinPipeNet(IPipeNet pipeNet)
        {
            _pipeNet = pipeNet;
            OnConnectedDirectionsNeedsUpdating();
        }

        public void ClearPipeNet()
        {
            _pipeNet = PipeNet.NullNet;
            OnConnectedDirectionsNeedsUpdating();
        }

        /// <summary>
        ///     Rotates the <see cref="PipeDirection"/> when the entity is rotated, and re-calculates the <see cref="IPipeNet"/>.
        /// </summary>
        void IRotatableNode.RotateEvent(RotateEvent ev)
        {
            if (!RotationsEnabled) return;
            var diff = ev.NewRotation - ev.OldRotation;
            PipeDirection = PipeDirection.RotatePipeDirection(diff);
            RefreshNodeGroup();
            OnConnectedDirectionsNeedsUpdating();
            UpdateAppearance();
        }

        protected override IEnumerable<Node> GetReachableNodes()
        {
            for (var i = 0; i < PipeDirectionHelpers.AllPipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);

                if (!PipeDirection.HasDirection(pipeDir))
                    continue;

                foreach (var pipe in LinkableNodesInDirection(pipeDir))
                {
                    yield return pipe;
                }
            }

            if (!ConnectionsEnabled || !ConnectToContainedEntities || !Owner.TryGetComponent(out ContainerManagerComponent? containerManager))
                yield break;

            // TODO ATMOS Kill it with fire.
            foreach (var container in containerManager.GetAllContainers())
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (!entity.TryGetComponent(out NodeContainerComponent? nodeContainer))
                        continue;

                    foreach (var node in nodeContainer.Nodes.Values)
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the pipes that can connect to us from entities on the tile or adjacent in a direction.
        /// </summary>
        private IEnumerable<PipeNode> LinkableNodesInDirection(PipeDirection pipeDir)
        {
            if (!Anchored)
                yield break;

            if (pipeDir is PipeDirection.Port or PipeDirection.Connector)
            {
                foreach (var pipe in PipesInTile())
                {
                    if (pipe.Anchored && pipe.ConnectionsEnabled && pipe.PipeDirection.HasDirection(pipeDir.GetOpposite()))
                        yield return pipe;
                }

                yield break;
            }

            foreach (var pipe in PipesInDirection(pipeDir))
            {
                if (pipe.ConnectionsEnabled && pipe.PipeDirection.HasDirection(pipeDir.GetOpposite()))
                    yield return pipe;
            }
        }

        /// <summary>
        ///     Gets the pipes from entities on the tile adjacent in a direction.
        /// </summary>
        private IEnumerable<PipeNode> PipesInDirection(PipeDirection pipeDir)
        {
            if (!Owner.Transform.Anchored)
                yield break;

            var grid = IoCManager.Resolve<IMapManager>().GetGrid(Owner.Transform.GridID);
            var position = Owner.Transform.Coordinates;
            foreach (var entity in grid.GetInDir(position, pipeDir.ToDirection()))
            {
                if (!Owner.EntityManager.ComponentManager.TryGetComponent<NodeContainerComponent>(entity, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    if (node is PipeNode pipe)
                        yield return pipe;
                }
            }
        }

        /// <summary>
        ///     Gets the pipes from entities on the tile adjacent in a direction.
        /// </summary>
        private IEnumerable<PipeNode> PipesInTile()
        {
            if (!Owner.Transform.Anchored)
                yield break;

            var grid = IoCManager.Resolve<IMapManager>().GetGrid(Owner.Transform.GridID);
            var position = Owner.Transform.Coordinates;
            foreach (var entity in grid.GetLocal(position))
            {
                if (!Owner.EntityManager.ComponentManager.TryGetComponent<NodeContainerComponent>(entity, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    if (node is PipeNode pipe)
                        yield return pipe;
                }
            }
        }

        /// <summary>
        ///     Updates the <see cref="ConnectedDirections"/> of this and all sorrounding pipes.
        /// </summary>
        private void OnConnectedDirectionsNeedsUpdating()
        {
            UpdateConnectedDirections();
            UpdateAdjacentConnectedDirections();
            UpdateAppearance();
        }

        /// <summary>
        ///     Checks what directions there are connectable pipes in, to update <see cref="ConnectedDirections"/>.
        /// </summary>
        private void UpdateConnectedDirections()
        {
            ConnectedDirections = PipeDirection.None;

            for (var i = 0; i < PipeDirectionHelpers.AllPipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);

                if (!PipeDirection.HasDirection(pipeDir))
                    continue;

                foreach (var pipe in LinkableNodesInDirection(pipeDir))
                {
                    if (pipe.Connectable && pipe.NodeGroupID == NodeGroupID)
                    {
                        ConnectedDirections |= pipeDir;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Calls <see cref="UpdateConnectedDirections"/> on all adjacent pipes,
        ///     to update their <see cref="ConnectedDirections"/> when this pipe is changed.
        /// </summary>
        private void UpdateAdjacentConnectedDirections()
        {
            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var pipeDir = (PipeDirection) (1 << i);

                foreach (var pipe in LinkableNodesInDirection(pipeDir))
                {
                    pipe.UpdateConnectedDirections();
                    pipe.UpdateAppearance();
                }
            }
        }

        /// <summary>
        ///     Updates the <see cref="AppearanceComponent"/>.
        ///     Gets the combined <see cref="ConnectedDirections"/> of every pipe on this entity, so the visualizer on this entity can draw the pipe connections.
        /// </summary>
        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance)
                || !Owner.TryGetComponent(out NodeContainerComponent? container))
                return;

            var netConnectedDirections = PipeDirection.None;

            foreach (var node in container.Nodes.Values)
            {
                if (node is PipeNode pipe)
                {
                    netConnectedDirections |= pipe.ConnectedDirections;
                }
            }

            appearance.SetData(PipeVisuals.VisualState, new PipeVisualState(netConnectedDirections));
        }
    }
}
