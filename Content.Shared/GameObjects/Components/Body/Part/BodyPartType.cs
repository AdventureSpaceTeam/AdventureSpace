﻿using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    /// <summary>
    ///     Defines the type of a <see cref="IBodyPart"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyPartType
    {
        Other = 0,
        Torso,
        Head,
        Arm,
        Hand,
        Leg,
        Foot
    }
}
