﻿using System;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class GasValveVisualizer : EnabledAtmosDeviceVisualizer
    {
        protected override object LayerMap => Layers.Enabled;
        protected override Enum DataKey => FilterVisuals.Enabled;

        enum Layers : byte
        {
            Enabled,
        }
    }
}
