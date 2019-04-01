﻿﻿using Content.Shared.GameObjects;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Physics;
using SS14.Shared.Serialization;
using System;
 using Content.Server.GameObjects.Components.Sound;
 using SS14.Shared.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Power;
using Content.Shared.Interfaces;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan
{
    public class HitscanWeaponComponent : Component, IAttackby
    {
        private const float MaxLength = 20;
        public override string Name => "HitscanWeapon";

        string _spritename;
        private int _damage;
        private int _baseFireCost;
        private float _lowerChargeLimit;

        //As this is a component that sits on the weapon rather than a static value
        //we just declare the field and then use GetComponent later to actually get it.
        //Do remember to add it in both the .yaml prototype and the factory in EntryPoint.cs
        //Otherwise you will get errors
        private HitscanWeaponCapacitorComponent capacitorComponent;

        public int Damage => _damage;

        public int BaseFireCost => _baseFireCost;

        public HitscanWeaponCapacitorComponent CapacitorComponent => capacitorComponent;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spritename, "sprite", "Objects/laser.png");
            serializer.DataField(ref _damage, "damage", 10);
            serializer.DataField(ref _baseFireCost, "baseFireCost", 300);
            serializer.DataField(ref _lowerChargeLimit, "lowerChargeLimit", 10);
        }

        public override void Initialize()
        {
            base.Initialize();
            var rangedWeapon = Owner.GetComponent<RangedWeaponComponent>();
            capacitorComponent = Owner.GetComponent<HitscanWeaponCapacitorComponent>();
            rangedWeapon.FireHandler = Fire;
        }

        public bool Attackby(IEntity user, IEntity attackwith)
        {
            if (!attackwith.TryGetComponent(out PowerStorageComponent component))
            {
                return false;
            }
            if (capacitorComponent.Full)
            {
                Owner.PopupMessage(user, "Capacitor at max charge");
                return false;
            }
            capacitorComponent.FillFrom(component);
            return true;
        }

        private void Fire(IEntity user, GridCoordinates clickLocation)
        {
            if (capacitorComponent.Charge < _lowerChargeLimit)
            {//If capacitor has less energy than the lower limit, do nothing
                return;
            }
            float energyModifier = capacitorComponent.GetChargeFrom(_baseFireCost) / _baseFireCost;
            var userPosition = user.Transform.WorldPosition; //Remember world positions are ephemeral and can only be used instantaneously
            var angle = new Angle(clickLocation.Position - userPosition);

            var ray = new Ray(userPosition, angle.ToVec());
            var rayCastResults = IoCManager.Resolve<IPhysicsManager>().IntersectRay(ray, MaxLength,
                Owner.Transform.GetMapTransform().Owner);

            Hit(rayCastResults, energyModifier);
            AfterEffects(user, rayCastResults, angle, energyModifier);
        }

        protected virtual void Hit(RayCastResults ray, float damageModifier)
        {
            if (ray.HitEntity != null && ray.HitEntity.TryGetComponent(out DamageableComponent damage))
            {
                damage.TakeDamage(DamageType.Heat, (int)Math.Round(_damage * damageModifier, MidpointRounding.AwayFromZero));
                //I used Math.Round over Convert.toInt32, as toInt32 always rounds to
                //even numbers if halfway between two numbers, rather than rounding to nearest
            }
        }

        protected virtual void AfterEffects(IEntity user, RayCastResults ray, Angle angle, float energyModifier)
        {
            var time = IoCManager.Resolve<IGameTiming>().CurTime;
            var dist = ray.DidHitObject ? ray.Distance : MaxLength;
            var offset = angle.ToVec() * dist / 2;
            var message = new EffectSystemMessage
            {
                EffectSprite = _spritename,
                Born = time,
                DeathTime = time + TimeSpan.FromSeconds(1),
                Size = new Vector2(dist, 1f),
                Coordinates = user.Transform.GridPosition.Translated(offset),
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), energyModifier),

                Shaded = false
            };
            var mgr = IoCManager.Resolve<IEntitySystemManager>();
            mgr.GetEntitySystem<EffectSystem>().CreateParticle(message);
            Owner.GetComponent<SoundComponent>().Play("/Audio/laser.ogg", AudioParams.Default.WithVolume(-5));
        }
    }
}
