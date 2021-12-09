﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class EntityAnchored : IGraphCondition
    {
        [DataField("anchored")] public bool Anchored { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            var transform = entityManager.GetComponent<TransformComponent>(uid);
            return transform.Anchored && Anchored || !transform.Anchored && !Anchored;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            var anchored = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity).Anchored;

            switch (Anchored)
            {
                case true when !anchored:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-entity-anchored"));
                    return true;
                case false when anchored:
                    args.PushMarkup(Loc.GetString("construction-examine-condition-entity-unanchored"));
                    return true;
            }

            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Anchored
                    ? "construction-step-condition-entity-anchored"
                    : "construction-step-condition-entity-unanchored",
                Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Tools/wrench.rsi"), "icon"),
            };
        }
    }
}
