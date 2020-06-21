﻿using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ProjectileComponent : Component, ICollideSpecial, ICollideBehavior
    {
        public override string Name => "Projectile";

        public bool IgnoreShooter = true;

        private EntityUid _shooter = EntityUid.Invalid;

        private Dictionary<DamageType, int> _damages;

        [ViewVariables]
        public Dictionary<DamageType, int> Damages
        {
            get => _damages;
            set => _damages = value;
        }
        
        public bool DeleteOnCollide => _deleteOnCollide;
        private bool _deleteOnCollide;

        // Get that juicy FPS hit sound
        private string _soundHit;
        private string _soundHitSpecies;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _deleteOnCollide, "delete_on_collide", true);
            // If not specified 0 damage
            serializer.DataField(ref _damages, "damages", new Dictionary<DamageType, int>());
            serializer.DataField(ref _soundHit, "soundHit", null);
            serializer.DataField(ref _soundHitSpecies, "soundHitSpecies", null);
        }

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(IEntity shooter)
        {
            _shooter = shooter.Uid;
        }

        /// <summary>
        /// Special collision override, can be used to give custom behaviors deciding when to collide
        /// </summary>
        /// <param name="collidedwith"></param>
        /// <returns></returns>
        bool ICollideSpecial.PreventCollide(IPhysBody collidedwith)
        {
            if (IgnoreShooter && collidedwith.Owner.Uid == _shooter)
                return true;
            return false;
        }

        /// <summary>
        /// Applies the damage when our projectile collides with its victim
        /// </summary>
        /// <param name="entity"></param>
        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (_soundHitSpecies != null && entity.HasComponent<SpeciesComponent>())
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHitSpecies, entity.Transform.GridPosition);
            } else if (_soundHit != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHit, entity.Transform.GridPosition);
            }
            
            if (entity.TryGetComponent(out DamageableComponent damage))
            {
                Owner.EntityManager.TryGetEntity(_shooter, out var shooter);

                foreach (var (damageType, amount) in _damages)
                {
                    damage.TakeDamage(damageType, amount, Owner, shooter);
                }
            }

            if (!entity.Deleted && entity.TryGetComponent(out CameraRecoilComponent recoilComponent)
                                && Owner.TryGetComponent(out PhysicsComponent physicsComponent))
            {
                var direction = physicsComponent.LinearVelocity.Normalized;
                recoilComponent.Kick(direction);
            }
        }

        void ICollideBehavior.PostCollide(int collideCount)
        {
            if (collideCount > 0 && DeleteOnCollide) Owner.Delete();
        }
    }
}
