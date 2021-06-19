﻿using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public class GasMixtureHolderComponent : Component, IGasMixtureHolder
    {
        public override string Name => "GasMixtureHolder";

        [ViewVariables] [DataField("air")] public GasMixture Air { get; set; } = new GasMixture();
    }
}
