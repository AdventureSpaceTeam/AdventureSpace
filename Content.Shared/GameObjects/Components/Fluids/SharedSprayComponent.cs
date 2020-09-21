﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Fluids
{
    public class SharedSprayComponent : Component
    {
        public override string Name => "Spray";
    }

    [Serializable, NetSerializable]
    public enum SprayVisuals
    {
        Safety,
    }
}
