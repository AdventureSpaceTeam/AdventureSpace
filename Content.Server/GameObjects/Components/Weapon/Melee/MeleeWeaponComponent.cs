﻿using Robust.Shared.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Content.Shared.GameObjects;
using Robust.Shared.Interfaces.Map;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    public class MeleeWeaponComponent : Component, IAfterAttack
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
#pragma warning restore 649

        public override string Name => "MeleeWeapon";

        public int Damage = 1;
        public float Range = 1;
        public float ArcWidth = 90;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref Damage, "damage", 5);
            serializer.DataField(ref Range, "range", 1);
            serializer.DataField(ref ArcWidth, "arcwidth", 90);
        }

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            var location = eventArgs.User.Transform.GridPosition;
            var angle = new Angle(eventArgs.ClickLocation.ToWorld(_mapManager).Position - location.ToWorld(_mapManager).Position);
            var entities = _serverEntityManager.GetEntitiesInArc(eventArgs.User.Transform.GridPosition, Range, angle, ArcWidth);

            foreach (var entity in entities)
            {
                if (!entity.Transform.IsMapTransform || entity == eventArgs.User)
                    continue;

                if (entity.TryGetComponent(out DamageableComponent damagecomponent))
                {
                    damagecomponent.TakeDamage(DamageType.Brute, Damage);
                }
            }
        }
    }
}
