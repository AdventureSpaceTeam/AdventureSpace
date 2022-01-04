﻿using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    public class BodyPartComponent : SharedBodyPartComponent
    {
    }
}
