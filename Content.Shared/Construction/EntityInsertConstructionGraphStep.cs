﻿using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public abstract class EntityInsertConstructionGraphStep : ConstructionGraphStep
    {
        public string Store { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Store, "store", string.Empty);
        }

        public abstract bool EntityValid(IEntity entity);
    }
}
