﻿using Content.Server.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReactionEffects
{
    [UsedImplicitly]
    [DataDefinition]
    public class SmokeAreaReactionEffect : AreaReactionEffect
    {
        protected override SolutionAreaEffectComponent? GetAreaEffectComponent(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().GetComponentOrNull<SmokeSolutionAreaEffectComponent>(entity);
        }
    }
}
