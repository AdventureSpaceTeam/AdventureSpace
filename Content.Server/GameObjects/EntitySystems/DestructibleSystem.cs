﻿using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class DestructibleSystem : EntitySystem
    {
        [Dependency] public readonly IRobustRandom Random = default!;

        public AudioSystem AudioSystem { get; private set; }

        public ActSystem ActSystem { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            AudioSystem = Get<AudioSystem>();
            ActSystem = Get<ActSystem>();
        }
    }
}
