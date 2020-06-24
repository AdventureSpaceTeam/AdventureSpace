using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Weapon;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Projectiles
{
    /// <summary>
    /// Upon colliding with an object this will flash in an area around it
    /// </summary>
    [RegisterComponent]
    public class FlashProjectileComponent : Component, ICollideBehavior
    {
        public override string Name => "FlashProjectile";

        private double _range;
        private double _duration;

        private bool _flashed;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _range, "range", 1.0);
            serializer.DataField(ref _duration, "duration", 8.0);
        }

        public override void Initialize()
        {
            base.Initialize();
            // Shouldn't be using this without a ProjectileComponent because it will just immediately collide with thrower
            if (!Owner.HasComponent<ProjectileComponent>())
            {
                throw new InvalidOperationException();
            }
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (_flashed)
            {
                return;
            }
            ServerFlashableComponent.FlashAreaHelper(Owner, _range, _duration);
            _flashed = true;
        }

        // Projectile should handle the deleting
        void ICollideBehavior.PostCollide(int collisionCount)
        {
            return;
        }
    }
}
