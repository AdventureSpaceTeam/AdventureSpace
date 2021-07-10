#nullable enable
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public class PowerSupplierComponent : BasePowerNetComponent
    {
        public override string Name => "PowerSupplier";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("supplyRate")]
        public float MaxSupply { get => NetworkSupply.MaxSupply; set => NetworkSupply.MaxSupply = value; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("supplyRampTolerance")]
        public float SupplyRampTolerance
        {
            get => NetworkSupply.SupplyRampTolerance;
            set => NetworkSupply.SupplyRampTolerance = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("supplyRampRate")]
        public float SupplyRampRate
        {
            get => NetworkSupply.SupplyRampRate;
            set => NetworkSupply.SupplyRampRate = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("supplyRampPosition")]
        public float SupplyRampPosition
        {
            get => NetworkSupply.SupplyRampPosition;
            set => NetworkSupply.SupplyRampPosition = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled
        {
            get => NetworkSupply.Enabled;
            set => NetworkSupply.Enabled = value;
        }

        [ViewVariables] public float CurrentSupply => NetworkSupply.CurrentSupply;

        [ViewVariables]
        public PowerState.Supply NetworkSupply { get; } = new();

        protected override void AddSelfToNet(IPowerNet powerNet)
        {
            powerNet.AddSupplier(this);
        }

        protected override void RemoveSelfFromNet(IPowerNet powerNet)
        {
            powerNet.RemoveSupplier(this);
        }
    }
}
