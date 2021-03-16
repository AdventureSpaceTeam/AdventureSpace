﻿using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ProjectileComponent : SharedProjectileComponent, IStartCollide
    {
        protected override EntityUid Shooter => _shooter;

        private EntityUid _shooter = EntityUid.Invalid;

        [DataField("damages")] private Dictionary<DamageType, int> _damages = new();

        [ViewVariables]
        public Dictionary<DamageType, int> Damages
        {
            get => _damages;
            set => _damages = value;
        }

        [field: DataField("delete_on_collide")]
        public bool DeleteOnCollide { get; } = true;

        // Get that juicy FPS hit sound
        [DataField("soundHit")]
        private string? _soundHit = default;
        [DataField("soundHitSpecies")]
        private string? _soundHitSpecies = default;

        private bool _damagedEntity;

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(IEntity shooter)
        {
            _shooter = shooter.Uid;
            Dirty();
        }

        /// <summary>
        ///     Applies the damage when our projectile collides with its victim
        /// </summary>
        void IStartCollide.CollideWith(IPhysBody ourBody, IPhysBody otherBody, in Manifold manifold)
        {
            // This is so entities that shouldn't get a collision are ignored.
            if (!otherBody.Hard || _damagedEntity)
            {
                return;
            }

            if (otherBody.Entity.TryGetComponent(out IDamageableComponent? damage) && _soundHitSpecies != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHitSpecies, otherBody.Entity.Transform.Coordinates);
            }
            else if (_soundHit != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHit, otherBody.Entity.Transform.Coordinates);
            }

            if (damage != null)
            {
                Owner.EntityManager.TryGetEntity(_shooter, out var shooter);

                foreach (var (damageType, amount) in _damages)
                {
                    damage.ChangeDamage(damageType, amount, false, shooter);
                }

                _damagedEntity = true;
            }

            // Damaging it can delete it
            if (!otherBody.Entity.Deleted && otherBody.Entity.TryGetComponent(out CameraRecoilComponent? recoilComponent))
            {
                var direction = ourBody.LinearVelocity.Normalized;
                recoilComponent.Kick(direction);
            }

            Owner.Delete();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ProjectileComponentState(NetID!.Value, _shooter, IgnoreShooter);
        }
    }
}
