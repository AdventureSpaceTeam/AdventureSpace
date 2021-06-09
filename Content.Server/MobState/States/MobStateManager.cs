﻿using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Server.MobState.States
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMobStateComponent))]
    [ComponentReference(typeof(IMobStateComponent))]
    public class MobStateComponent : SharedMobStateComponent
    {
    }
}
