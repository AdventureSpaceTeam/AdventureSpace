﻿using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and
    ///     "ruins" or "destroys" it after enough damage is taken.
    /// </summary>
    [ComponentReference(typeof(IDamageableComponent))]
    public abstract class RuinableComponent : DamageableComponent
    {
        private DamageState _currentDamageState;

        /// <summary>
        ///     How much HP this component can sustain before triggering
        ///     <see cref="PerformDestruction"/>.
        /// </summary>
        public int MaxHp { get; private set; }

        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        protected string DestroySound { get; private set; }

        public override List<DamageState> SupportedDamageStates =>
            new List<DamageState> {DamageState.Alive, DamageState.Dead};

        public override DamageState CurrentDamageState => _currentDamageState;

        public override void Initialize()
        {
            base.Initialize();
            HealthChangedEvent += OnHealthChanged;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, ruinable => ruinable.MaxHp, "maxHP", 100);
            serializer.DataField(this, ruinable => ruinable.DestroySound, "destroySound", string.Empty);
        }

        public override void OnRemove()
        {
            base.OnRemove();
            HealthChangedEvent -= OnHealthChanged;
        }

        private void OnHealthChanged(HealthChangedEventArgs e)
        {
            if (CurrentDamageState != DamageState.Dead && TotalDamage >= MaxHp)
            {
                PerformDestruction();
            }
        }

        /// <summary>
        ///     Destroys the Owner <see cref="IEntity"/>, setting
        ///     <see cref="IDamageableComponent.CurrentDamageState"/> to
        ///     <see cref="DamageState.Dead"/>
        /// </summary>
        protected void PerformDestruction()
        {
            _currentDamageState = DamageState.Dead;

            if (!Owner.Deleted && DestroySound != string.Empty)
            {
                var pos = Owner.Transform.GridPosition;
                EntitySystem.Get<AudioSystem>().PlayAtCoords(DestroySound, pos);
            }

            DestructionBehavior();
        }

        protected abstract void DestructionBehavior();
    }
}
