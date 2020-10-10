#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Damage
{
    public interface IDamageableComponent : IComponent, IExAct
    {
        /// <summary>
        ///     Called when the entity's <see cref="IDamageableComponent"/> values change.
        ///     Of note is that a "deal 0 damage" call will still trigger this event
        ///     (including both damage negated by resistance or simply inputting 0 as
        ///     the amount of damage to deal).
        /// </summary>
        event Action<HealthChangedEventArgs> HealthChangedEvent;

        Dictionary<DamageState, int> Thresholds { get; }

        /// <summary>
        ///     List of all <see cref="Damage.DamageState">DamageStates</see> that
        ///     <see cref="CurrentState"/> can be.
        /// </summary>
        List<DamageState> SupportedDamageStates { get; }

        /// <summary>
        ///     The <see cref="Damage.DamageState"/> currently representing this component.
        /// </summary>
        DamageState CurrentState { get; set; }

        /// <summary>
        ///     Sum of all damages taken.
        /// </summary>
        int TotalDamage { get; }

        /// <summary>
        ///     The amount of damage mapped by <see cref="DamageClass"/>.
        /// </summary>
        IReadOnlyDictionary<DamageClass, int> DamageClasses { get; }

        /// <summary>
        ///     The amount of damage mapped by <see cref="DamageType"/>.
        /// </summary>
        IReadOnlyDictionary<DamageType, int> DamageTypes { get; }

        /// <summary>
        ///     The damage flags on this component.
        /// </summary>
        DamageFlag Flags { get; }

        /// <summary>
        ///     Adds a flag to this component.
        /// </summary>
        /// <param name="flag">The flag to add.</param>
        void AddFlag(DamageFlag flag);

        /// <summary>
        ///     Checks whether or not this component has a specific flag.
        /// </summary>
        /// <param name="flag">The flag to check for.</param>
        /// <returns>True if it has the flag, false otherwise.</returns>
        bool HasFlag(DamageFlag flag);

        /// <summary>
        ///     Removes a flag from this component.
        /// </summary>
        /// <param name="flag">The flag to remove.</param>
        void RemoveFlag(DamageFlag flag);

        /// <summary>
        ///     Gets the amount of damage of a type.
        /// </summary>
        /// <param name="type">The type to get the damage of.</param>
        /// <param name="damage">The amount of damage of that type.</param>
        /// <returns>
        ///     True if the given <see cref="type"/> is supported, false otherwise.
        /// </returns>
        bool TryGetDamage(DamageType type, [NotNullWhen(true)] out int damage);

        /// <summary>
        ///     Changes the specified <see cref="DamageType"/>, applying
        ///     resistance values only if it is damage.
        /// </summary>
        /// <param name="type">Type of damage being changed.</param>
        /// <param name="amount">
        ///     Amount of damage being received (positive for damage, negative for heals).
        /// </param>
        /// <param name="ignoreResistances">
        ///     Whether or not to ignore resistances.
        ///     Healing always ignores resistances, regardless of this input.
        /// </param>
        /// <param name="source">
        ///     The entity that dealt or healed the damage, if any.
        /// </param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require, such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     False if the given type is not supported or improper
        ///     <see cref="HealthChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool ChangeDamage(DamageType type, int amount, bool ignoreResistances, IEntity? source = null,
            HealthChangeParams? extraParams = null);

        /// <summary>
        ///     Changes the specified <see cref="DamageClass"/>, applying
        ///     resistance values only if it is damage.
        ///     Spreads amount evenly between the <see cref="DamageType"></see>s
        ///     represented by that class.
        /// </summary>
        /// <param name="class">Class of damage being changed.</param>
        /// <param name="amount">
        ///     Amount of damage being received (positive for damage, negative for heals).
        /// </param>
        /// <param name="ignoreResistances">
        ///     Whether to ignore resistances.
        ///     Healing always ignores resistances, regardless of this input.
        /// </param>
        /// <param name="source">Entity that dealt or healed the damage, if any.</param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require,
        ///     such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     Returns false if the given class is not supported or improper
        ///     <see cref="HealthChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool ChangeDamage(DamageClass @class, int amount, bool ignoreResistances, IEntity? source = null,
            HealthChangeParams? extraParams = null);

        /// <summary>
        ///     Forcefully sets the specified <see cref="DamageType"/> to the given
        ///     value, ignoring resistance values.
        /// </summary>
        /// <param name="type">Type of damage being changed.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <param name="source">Entity that set the new damage value.</param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require,
        ///     such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     Returns false if the given type is not supported or improper
        ///     <see cref="HealthChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool SetDamage(DamageType type, int newValue, IEntity? source = null, HealthChangeParams? extraParams = null);

        /// <summary>
        ///     Sets all damage values to zero.
        /// </summary>
        void Heal();

        /// <summary>
        ///     Invokes the HealthChangedEvent with the current values of health.
        /// </summary>
        void ForceHealthChangedEvent();

        /// <summary>
        ///     Calculates the health of an entity until it enters
        ///     <see cref="threshold"/>.
        /// </summary>
        /// <param name="threshold">The state to use as a threshold.</param>
        /// <returns>
        ///     The current and maximum health on this entity based on
        ///     <see cref="threshold"/>, or null if the state is not supported.
        /// </returns>
        (int current, int max)? Health(DamageState threshold);

        /// <summary>
        ///     Calculates the health of an entity until it enters
        ///     <see cref="threshold"/>.
        /// </summary>
        /// <param name="threshold">The state to use as a threshold.</param>
        /// <param name="health">
        ///     The current and maximum health on this entity based on
        ///     <see cref="threshold"/>, or null if the state is not supported.
        /// </param>
        /// <returns>
        ///     True if <see cref="threshold"/> is supported, false otherwise.
        /// </returns>
        bool TryHealth(DamageState threshold, [NotNullWhen(true)] out (int current, int max) health);
    }

    /// <summary>
    ///     Data class with information on how to damage a
    ///     <see cref="IDamageableComponent"/>.
    ///     While not necessary to damage for all instances, classes such as
    ///     <see cref="SharedBodyComponent"/> may require it for extra data
    ///     (such as selecting which limb to target).
    /// </summary>
    public class HealthChangeParams : EventArgs
    {
    }

    /// <summary>
    ///     Data class with information on how the <see cref="DamageType"/>
    ///     values of a <see cref="IDamageableComponent"/> have changed.
    /// </summary>
    public class HealthChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     Reference to the <see cref="IDamageableComponent"/> that invoked the event.
        /// </summary>
        public readonly IDamageableComponent Damageable;

        /// <summary>
        ///     List containing data on each <see cref="DamageType"/> that was changed.
        /// </summary>
        public readonly List<HealthChangeData> Data;

        public HealthChangedEventArgs(IDamageableComponent damageable, List<HealthChangeData> data)
        {
            Damageable = damageable;
            Data = data;
        }

        public HealthChangedEventArgs(IDamageableComponent damageable, DamageType type, int newValue, int delta)
        {
            Damageable = damageable;

            var datum = new HealthChangeData(type, newValue, delta);
            var data = new List<HealthChangeData> {datum};

            Data = data;
        }
    }

    /// <summary>
    ///     Data class with information on how the value of a
    ///     single <see cref="DamageType"/> has changed.
    /// </summary>
    public struct HealthChangeData
    {
        /// <summary>
        ///     Type of damage that changed.
        /// </summary>
        public DamageType Type;

        /// <summary>
        ///     The new current value for that damage.
        /// </summary>
        public int NewValue;

        /// <summary>
        ///     How much the health value changed from its last value (negative is heals, positive is damage).
        /// </summary>
        public int Delta;

        public HealthChangeData(DamageType type, int newValue, int delta)
        {
            Type = type;
            NewValue = newValue;
            Delta = delta;
        }
    }
}
