﻿using Content.Shared.Body.Mechanism;
using Robust.Shared.GameObjects;

namespace Content.Client.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMechanismComponent))]
    public class MechanismComponent : SharedMechanismComponent
    {
    }
}
