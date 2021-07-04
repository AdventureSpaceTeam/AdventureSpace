#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.AME.Components;
using Content.Server.Explosion;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;

namespace Content.Server.AME
{
    /// <summary>
    /// Node group class for handling the Antimatter Engine's console and parts.
    /// </summary>
    [NodeGroup(NodeGroupID.AMEngine)]
    public class AMENodeGroup : BaseNodeGroup
    {
        /// <summary>
        /// The AME controller which is currently in control of this node group.
        /// This could be tracked a few different ways, but this is most convenient,
        /// since any part connected to the node group can easily find the master.
        /// </summary>
        [ViewVariables]
        private AMEControllerComponent? _masterController;

        [Dependency]
        private readonly IRobustRandom _random = default!;

        public AMEControllerComponent? MasterController => _masterController;

        private readonly List<AMEShieldComponent> _cores = new();

        public int CoreCount => _cores.Count;

        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);

            foreach (var node in groupNodes)
            {
                if (node.Owner.TryGetComponent(out AMEControllerComponent? controller))
                {
                    _masterController = controller;
                }
            }
        }

        public override void RemoveNode(Node node)
        {
            base.RemoveNode(node);

            RefreshAMENodes(_masterController);
            if (_masterController != null && _masterController?.Owner == node.Owner) { _masterController = null; }
        }

        public void RefreshAMENodes(AMEControllerComponent? controller)
        {
            if(_masterController == null && controller != null)
            {
                _masterController = controller;
            }

            foreach (AMEShieldComponent core in _cores)
            {
                core.UnsetCore();
            }
            _cores.Clear();

            //Check each shield node to see if it meets core criteria
            foreach (Node node in Nodes)
            {
                var nodeOwner = node.Owner;
                if (!nodeOwner.TryGetComponent<AMEShieldComponent>(out var shield)) { continue; }

                var grid = IoCManager.Resolve<IMapManager>().GetGrid(nodeOwner.Transform.GridID);
                var nodeNeighbors = grid.GetCellsInSquareArea(nodeOwner.Transform.Coordinates, 1)
                    .Select(sgc => nodeOwner.EntityManager.GetEntity(sgc))
                    .Where(entity => entity != nodeOwner)
                    .Select(entity => entity.TryGetComponent<AMEShieldComponent>(out var adjshield) ? adjshield : null)
                    .Where(adjshield => adjshield != null);

                if (nodeNeighbors.Count() >= 8)
                {
                    _cores.Add(shield);
                }
            }

            foreach (AMEShieldComponent core in _cores)
            {
                core.SetCore();
            }
        }

        public void UpdateCoreVisuals(int injectionAmount, bool injecting)
        {

            var injectionStrength = CoreCount > 0 ? injectionAmount / CoreCount : 0;

            foreach (AMEShieldComponent core in _cores)
            {
                core.UpdateCoreVisuals(injectionStrength, injecting);
            }
        }

        public int InjectFuel(int fuel, out bool overloading)
        {
            overloading = false;
            if(fuel > 0 && CoreCount > 0)
            {
                var safeFuelLimit = CoreCount * 2;
                if (fuel > safeFuelLimit)
                {
                    // The AME is being overloaded.
                    // Note about these maths: I would assume the general idea here is to make larger engines less safe to overload.
                    // In other words, yes, those are supposed to be CoreCount, not safeFuelLimit.
                    var instability = 0;
                    var overloadVsSizeResult = fuel - CoreCount;

                    // fuel > safeFuelLimit: Slow damage. Can safely run at this level for burst periods if the engine is small and someone is keeping an eye on it.
                    if (_random.Prob(0.5f))
                        instability = 1;
                    // overloadVsSizeResult > 5:
                    if (overloadVsSizeResult > 5)
                        instability = 5;
                    // overloadVsSizeResult > 10: This will explode in at most 5 injections.
                    if (overloadVsSizeResult > 10)
                        instability = 20;

                    // Apply calculated instability
                    if (instability != 0)
                    {
                        overloading = true;
                        foreach(AMEShieldComponent core in _cores)
                        {
                            core.CoreIntegrity -= instability;
                        }
                    }
                }
                // Note the float conversions. The maths will completely fail if not done using floats.
                return (int) ((((float) fuel) / CoreCount) * fuel * 20000);
            }
            return 0;
        }

        public int GetTotalStability()
        {
            if(CoreCount < 1) { return 100; }
            var stability = 0;

            foreach(AMEShieldComponent core in _cores)
            {
                stability += core.CoreIntegrity;
            }

            stability = stability / CoreCount;

            return stability;
        }

        public void ExplodeCores()
        {
            if(_cores.Count < 1 || MasterController == null) { return; }

            var intensity = 0;

            /*
             * todo: add an exact to the shielding and make this find the core closest to the controller
             * so they chain explode, after helpers have been added to make it not cancer
            */
            var epicenter = _cores.First();

            foreach (AMEShieldComponent core in _cores)
            {
                intensity += MasterController.InjectionAmount;
            }

            intensity = Math.Min(intensity, 8);

            epicenter.Owner.SpawnExplosion(intensity / 2, intensity, intensity * 2, intensity * 3);
        }
    }
}
