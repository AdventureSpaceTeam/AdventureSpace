using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Other.Components
{
    [RegisterComponent]
    public class GasMinerComponent : Component
    {
        public override string Name => "GasMiner";

        public bool Enabled { get; set; } = true;

        public bool Broken { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxExternalAmount")]
        public float MaxExternalAmount { get; set; } = float.PositiveInfinity;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxExternalPressure")]
        public float MaxExternalPressure { get; set; } = 6500f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnGas")]
        public Gas SpawnGas { get; set; } = Gas.Invalid;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnTemperature")]
        public float SpawnTemperature { get; set; } = Atmospherics.T20C;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnAmount")]
        public float SpawnAmount { get; set; } = Atmospherics.MolesCellStandard * 20f;
    }
}
