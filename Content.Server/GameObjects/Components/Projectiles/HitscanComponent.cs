using System;
using Content.Shared.GameObjects;
using Content.Shared.Physics;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Projectiles
{
    /// <summary>
    /// Lasers etc.
    /// </summary>
    [RegisterComponent]
    public class HitscanComponent : Component
    {
        public override string Name => "Hitscan";
        public CollisionGroup CollisionMask => (CollisionGroup) _collisionMask;
        private int _collisionMask;
        
        public float Damage
        {
            get => _damage;
            set => _damage = value;
        }
        private float _damage;
        public DamageType DamageType => _damageType;
        private DamageType _damageType;
        public float MaxLength => 20.0f;

        private TimeSpan _startTime;
        private TimeSpan _deathTime;

        public float ColorModifier { get; set; } = 1.0f;
        private string _spriteName;
        private string _muzzleFlash;
        private string _impactFlash;
        private string _soundHitWall;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _collisionMask, "layers", (int) CollisionGroup.Opaque, WithFormat.Flags<CollisionLayer>());
            serializer.DataField(ref _damage, "damage", 10.0f);
            serializer.DataField(ref _damageType, "damageType", DamageType.Heat);
            serializer.DataField(ref _spriteName, "spriteName", "Objects/Guns/Projectiles/laser.png");
            serializer.DataField(ref _muzzleFlash, "muzzleFlash", null);
            serializer.DataField(ref _impactFlash, "impactFlash", null);
            serializer.DataField(ref _soundHitWall, "soundHitWall", "/Audio/Guns/Hits/laser_sear_wall.ogg");
        }

        public void FireEffects(IEntity user, float distance, Angle angle, IEntity hitEntity = null)
        {
            var effectSystem = EntitySystem.Get<EffectSystem>();
            _startTime = IoCManager.Resolve<IGameTiming>().CurTime;
            _deathTime = _startTime + TimeSpan.FromSeconds(1);

            var afterEffect = AfterEffects(user.Transform.GridPosition, angle, distance, 1.0f);
            if (afterEffect != null)
            {
                effectSystem.CreateParticle(afterEffect);
            }

            // if we're too close we'll stop the impact and muzzle / impact sprites from clipping
            if (distance > 1.0f)
            {
                var impactEffect = ImpactFlash(distance, angle);
                if (impactEffect != null)
                {
                    effectSystem.CreateParticle(impactEffect);
                }

                var muzzleEffect = MuzzleFlash(user.Transform.GridPosition, angle);
                if (muzzleEffect != null)
                {
                    effectSystem.CreateParticle(muzzleEffect);
                }
            }

            if (hitEntity != null && _soundHitWall != null)
            {
                // TODO: No wall component so ?
                var offset = angle.ToVec().Normalized / 2;
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHitWall, user.Transform.GridPosition.Translated(offset));
            }
            
            Timer.Spawn((int) _deathTime.TotalMilliseconds, () =>
            {
                if (!Owner.Deleted)
                {
                    Owner.Delete();
                }
            });
        }
        
        private EffectSystemMessage MuzzleFlash(GridCoordinates grid, Angle angle)
        {
            if (_muzzleFlash == null)
            {
                return null;
            }
            
            var offset = angle.ToVec().Normalized / 2;
            
            var message = new EffectSystemMessage
            {
                EffectSprite = _muzzleFlash,
                Born = _startTime,
                DeathTime = _deathTime,
                Coordinates = grid.Translated(offset),
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };
            
            return message;
        }

        private EffectSystemMessage AfterEffects(GridCoordinates origin, Angle angle, float distance, float offset = 0.0f)
        {
            var midPointOffset = angle.ToVec() * distance / 2;
            var message = new EffectSystemMessage
            {
                EffectSprite = _spriteName,
                Born = _startTime,
                DeathTime = _deathTime,
                Size = new Vector2(distance - offset, 1f),
                Coordinates = origin.Translated(midPointOffset),
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
                ColorDelta = new Vector4(0, 0, 0, -1500f),

                Shaded = false
            };
            
            return message;
        }

        private EffectSystemMessage ImpactFlash(float distance, Angle angle)
        {
            if (_impactFlash == null)
            {
                return null;
            }

            var message = new EffectSystemMessage
            {
                EffectSprite = _impactFlash,
                Born = _startTime,
                DeathTime = _deathTime,
                Coordinates = Owner.Transform.GridPosition.Translated(angle.ToVec() * distance),
                //Rotated from east facing
                Rotation = (float) angle.FlipPositive(),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };

            return message;
        }
    }
}
