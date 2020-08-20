﻿#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Damage
{
    /// <summary>
    ///     Component that allows attached entities to take damage.
    ///     This basic version never dies (thus can take an indefinite amount of damage).
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class DamageableComponent : Component, IDamageableComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
#pragma warning restore 649

        public override string Name => "Damageable";

        public event Action<HealthChangedEventArgs> HealthChangedEvent = default!;

        [ViewVariables] private ResistanceSet Resistance { get; set; } = default!;

        [ViewVariables] private DamageContainer Damage { get; set; } = default!;

        public virtual List<DamageState> SupportedDamageStates => new List<DamageState> {DamageState.Alive};

        public virtual DamageState CurrentDamageState { get; protected set; } = DamageState.Alive;

        [ViewVariables] public int TotalDamage => Damage.TotalDamage;

        public IReadOnlyDictionary<DamageClass, int> DamageClasses => Damage.DamageClasses;

        public IReadOnlyDictionary<DamageType, int> DamageTypes => Damage.DamageTypes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                // Doesn't write to file, TODO?
                // Yes, TODO
                var containerId = "biologicalDamageContainer";
                var resistanceId = "defaultResistances";

                serializer.DataField(ref containerId, "damageContainer", "biologicalDamageContainer");
                serializer.DataField(ref resistanceId, "resistances", "defaultResistances");

                if (!_prototypeManager.TryIndex(containerId!, out DamageContainerPrototype damage))
                {
                    throw new InvalidOperationException(
                        $"No {nameof(DamageContainerPrototype)} found with name {containerId}");
                }

                Damage = new DamageContainer(OnHealthChanged, damage);

                if (!_prototypeManager.TryIndex(resistanceId!, out ResistanceSetPrototype resistance))
                {
                    throw new InvalidOperationException(
                        $"No {nameof(ResistanceSetPrototype)} found with name {resistanceId}");
                }

                Resistance = new ResistanceSet(resistance);
            }
        }

        public bool TryGetDamage(DamageType type, out int damage)
        {
            return Damage.TryGetDamageValue(type, out damage);
        }

        public bool ChangeDamage(DamageType type, int amount, bool ignoreResistances,
            IEntity? source = null,
            HealthChangeParams? extraParams = null)
        {
            if (Damage.SupportsDamageType(type))
            {
                var finalDamage = amount;
                if (!ignoreResistances)
                {
                    finalDamage = Resistance.CalculateDamage(type, amount);
                }

                Damage.ChangeDamageValue(type, finalDamage);

                return true;
            }

            return false;
        }

        public bool ChangeDamage(DamageClass @class, int amount, bool ignoreResistances,
            IEntity? source = null,
            HealthChangeParams? extraParams = null)
        {
            if (Damage.SupportsDamageClass(@class))
            {
                var types = @class.ToTypes();

                if (amount < 0)
                {
                    // Changing multiple types is a bit more complicated. Might be a better way (formula?) to do this,
                    // but essentially just loops between each damage category until all healing is used up.
                    var healingLeft = amount;
                    var healThisCycle = 1;

                    // While we have healing left...
                    while (healingLeft > 0 && healThisCycle != 0)
                    {
                        // Infinite loop fallback, if no healing was done in a cycle
                        // then exit
                        healThisCycle = 0;

                        int healPerType;
                        if (healingLeft > -types.Count && healingLeft < 0)
                        {
                            // Say we were to distribute 2 healing between 3
                            // this will distribute 1 to each (and stop after 2 are given)
                            healPerType = -1;
                        }
                        else
                        {
                            // Say we were to distribute 62 healing between 3
                            // this will distribute 20 to each, leaving 2 for next loop
                            healPerType = healingLeft / types.Count;
                        }

                        foreach (var type in types)
                        {
                            var healAmount =
                                Math.Max(Math.Max(healPerType, -Damage.GetDamageValue(type)),
                                    healingLeft);

                            Damage.ChangeDamageValue(type, healAmount);
                            healThisCycle += healAmount;
                            healingLeft -= healAmount;
                        }
                    }

                    return true;
                }

                var damageLeft = amount;

                while (damageLeft > 0)
                {
                    int damagePerType;

                    if (damageLeft < types.Count && damageLeft > 0)
                    {
                        damagePerType = 1;
                    }
                    else
                    {
                        damagePerType = damageLeft / types.Count;
                    }

                    foreach (var type in types)
                    {
                        var damageAmount = Math.Min(damagePerType, damageLeft);
                        Damage.ChangeDamageValue(type, damageAmount);
                        damageLeft -= damageAmount;
                    }
                }

                return true;
            }

            return false;
        }

        public bool SetDamage(DamageType type, int newValue, IEntity? source = null,
            HealthChangeParams? extraParams = null)
        {
            if (Damage.SupportsDamageType(type))
            {
                Damage.SetDamageValue(type, newValue);

                return true;
            }

            return false;
        }

        public void Heal()
        {
            Damage.Heal();
        }

        public void ForceHealthChangedEvent()
        {
            var data = new List<HealthChangeData>();

            foreach (var type in Damage.SupportedTypes)
            {
                var damage = Damage.GetDamageValue(type);
                var datum = new HealthChangeData(type, damage, 0);
                data.Add(datum);
            }

            OnHealthChanged(data);
        }

        private void OnHealthChanged(List<HealthChangeData> changes)
        {
            var args = new HealthChangedEventArgs(this, changes);
            OnHealthChanged(args);
        }

        protected virtual void OnHealthChanged(HealthChangedEventArgs e)
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, e);
            HealthChangedEvent?.Invoke(e);
            Dirty();
        }
    }
}
