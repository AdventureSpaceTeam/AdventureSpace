﻿#nullable enable
using System;
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SpriteChange : IGraphAction
    {
        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.SpriteSpecifier, "specifier", SpriteSpecifier.Invalid);
            serializer.DataField(this, x => x.Layer, "layer", 0);
        }

        public int Layer { get; private set; } = 0;
        public SpriteSpecifier? SpriteSpecifier { get; private set; } = SpriteSpecifier.Invalid;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || SpriteSpecifier == null || SpriteSpecifier == SpriteSpecifier.Invalid) return;

            if (!entity.TryGetComponent(out SpriteComponent? sprite)) return;

            // That layer doesn't exist, we do nothing.
            if (sprite.LayerCount <= Layer) return;

            sprite.LayerSetSprite(Layer, SpriteSpecifier);
        }
    }
}
