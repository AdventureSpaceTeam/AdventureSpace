﻿namespace Content.Shared.Damage
{
    /// <summary>
    ///     Data class with information on how the value of a
    ///     single <see cref="DamageTypePrototype"/> has changed.
    /// </summary>
    public struct DamageChangeData
    {
        /// <summary>
        ///     Type of damage that changed.
        /// </summary>
        public DamageTypePrototype Type;

        /// <summary>
        ///     The new current value for that damage.
        /// </summary>
        public int NewValue;

        /// <summary>
        ///     How much the health value changed from its last value (negative is heals, positive is damage).
        /// </summary>
        public int Delta;

        public DamageChangeData(DamageTypePrototype type, int newValue, int delta)
        {
            Type = type;
            NewValue = newValue;
            Delta = delta;
        }
    }
}
