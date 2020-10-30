﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedWindowComponent : Component
    {
        public override string Name => "Window";
    }

    [Serializable, NetSerializable]
    public enum WindowVisuals
    {
        Damage
    }
}
