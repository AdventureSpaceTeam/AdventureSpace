using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public class GasOutletInjectorComponent : Component
    {
        public override string Name => "GasOutletInjector";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Injecting { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public float VolumeRate { get; set; } = 50f;

        [DataField("inlet")]
        public string InletName { get; set; } = "pipe";

        // TODO ATMOS: Inject method.
    }
}
