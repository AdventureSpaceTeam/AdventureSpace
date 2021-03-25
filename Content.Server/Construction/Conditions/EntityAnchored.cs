﻿using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
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

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out IPhysBody? physics)) return false;

            return (physics.BodyType == BodyType.Static && Anchored) || (physics.BodyType != BodyType.Static && !Anchored);
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out IPhysBody? physics)) return false;

            switch (Anchored)
            {
                case true when physics.BodyType != BodyType.Static:
                    message.AddMarkup("First, anchor it.\n");
                    return true;
                case false when physics.BodyType == BodyType.Static:
                    message.AddMarkup("First, unanchor it.\n");
                    return true;
            }

            return false;
        }
    }
}
