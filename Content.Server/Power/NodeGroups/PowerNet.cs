#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Pow3r;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.NodeGroups
{
    public interface IPowerNet
    {
        void AddSupplier(PowerSupplierComponent supplier);

        void RemoveSupplier(PowerSupplierComponent supplier);

        void AddConsumer(PowerConsumerComponent consumer);

        void RemoveConsumer(PowerConsumerComponent consumer);

        void AddDischarger(BatteryDischargerComponent discharger);

        void RemoveDischarger(BatteryDischargerComponent discharger);

        void AddCharger(BatteryChargerComponent charger);

        void RemoveCharger(BatteryChargerComponent charger);
    }

    [NodeGroup(NodeGroupID.HVPower, NodeGroupID.MVPower)]
    [UsedImplicitly]
    public class PowerNet : BaseNetConnectorNodeGroup<BasePowerNetComponent, IPowerNet>, IPowerNet
    {
        private readonly PowerNetSystem _powerNetSystem = EntitySystem.Get<PowerNetSystem>();

        [ViewVariables] public readonly List<PowerSupplierComponent> Suppliers = new();
        [ViewVariables] public readonly List<PowerConsumerComponent> Consumers = new();
        [ViewVariables] public readonly List<BatteryChargerComponent> Chargers = new();
        [ViewVariables] public readonly List<BatteryDischargerComponent> Dischargers = new();

        [ViewVariables]
        public PowerState.Network NetworkNode { get; } = new();

        public override void Initialize(Node sourceNode)
        {
            base.Initialize(sourceNode);

            _powerNetSystem.InitPowerNet(this);
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            base.AfterRemake(newGroups);

            _powerNetSystem.DestroyPowerNet(this);
        }

        protected override void SetNetConnectorNet(BasePowerNetComponent netConnectorComponent)
        {
            netConnectorComponent.Net = this;
        }

        public void AddSupplier(PowerSupplierComponent supplier)
        {
            supplier.NetworkSupply.LinkedNetwork = default;
            Suppliers.Add(supplier);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void RemoveSupplier(PowerSupplierComponent supplier)
        {
            supplier.NetworkSupply.LinkedNetwork = default;
            Suppliers.Remove(supplier);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void AddConsumer(PowerConsumerComponent consumer)
        {
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Add(consumer);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void RemoveConsumer(PowerConsumerComponent consumer)
        {
            consumer.NetworkLoad.LinkedNetwork = default;
            Consumers.Remove(consumer);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void AddDischarger(BatteryDischargerComponent discharger)
        {
            var battery = discharger.Owner.GetComponent<PowerNetworkBatteryComponent>();
            battery.NetworkBattery.LinkedNetworkCharging = default;
            Dischargers.Add(discharger);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void RemoveDischarger(BatteryDischargerComponent discharger)
        {
            // Can be missing if the entity is being deleted, not a big deal.
            if (discharger.Owner.TryGetComponent(out PowerNetworkBatteryComponent? battery))
                battery.NetworkBattery.LinkedNetworkCharging = default;

            Dischargers.Remove(discharger);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void AddCharger(BatteryChargerComponent charger)
        {
            var battery = charger.Owner.GetComponent<PowerNetworkBatteryComponent>();
            battery.NetworkBattery.LinkedNetworkCharging = default;
            Chargers.Add(charger);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }

        public void RemoveCharger(BatteryChargerComponent charger)
        {
            // Can be missing if the entity is being deleted, not a big deal.
            if (charger.Owner.TryGetComponent(out PowerNetworkBatteryComponent? battery))
                battery.NetworkBattery.LinkedNetworkCharging = default;

            Chargers.Remove(charger);
            _powerNetSystem.QueueReconnectPowerNet(this);
        }
    }
}
