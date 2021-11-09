using System;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Reagent
{
    public enum ReactionMethod
    {
        Touch,
        Injection,
        Ingestion,
    }

    [ImplicitDataDefinitionForInheritors]
    public abstract class ReagentEntityReaction
    {
        [ViewVariables]
        [DataField("touch")]
        public bool Touch { get; } = false;

        [ViewVariables]
        [DataField("injection")]
        public bool Injection { get; } = false;

        [ViewVariables]
        [DataField("ingestion")]
        public bool Ingestion { get; } = false;

        public void React(ReactionMethod method, EntityUid uid, ReagentPrototype reagent, FixedPoint2 volume, Components.Solution? source, IEntityManager entityManager)
        {
            switch (method)
            {
                case ReactionMethod.Touch:
                    if (!Touch)
                        return;
                    break;
                case ReactionMethod.Injection:
                    if(!Injection)
                        return;
                    break;
                case ReactionMethod.Ingestion:
                    if(!Ingestion)
                        return;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }

            React(uid, reagent, volume, source, entityManager);
        }

        protected abstract void React(EntityUid uid, ReagentPrototype reagent, FixedPoint2 volume, Components.Solution? source, IEntityManager entityManager);
    }
}
