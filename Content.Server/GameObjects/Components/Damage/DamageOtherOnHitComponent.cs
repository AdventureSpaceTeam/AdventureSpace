using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    public class DamageOtherOnHitComponent : Component, IThrowCollide
    {
        public override string Name => "DamageOtherOnHit";

        private DamageType _damageType;
        private int _amount;
        private bool _ignoreResistances;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _damageType, "damageType", DamageType.Blunt);
            serializer.DataField(ref _amount, "amount", 1);
            serializer.DataField(ref _ignoreResistances, "ignoreResistances", false);
        }

        public void DoHit(ThrowCollideEventArgs eventArgs)
        {
            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent damageable)) return;

            damageable.ChangeDamage(_damageType, _amount, _ignoreResistances, eventArgs.User);
        }
    }
}
