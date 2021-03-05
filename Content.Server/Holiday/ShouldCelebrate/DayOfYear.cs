using System;
using Content.Server.Holiday.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Holiday.ShouldCelebrate
{
    /// <summary>
    ///     For a holiday that occurs on a certain day of the year.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class DayOfYear : IHolidayShouldCelebrate
    {
        [DataField("dayOfYear")]
        private uint _dayOfYear = 1;

        public bool ShouldCelebrate(DateTime date, HolidayPrototype holiday)
        {
            return date.DayOfYear == _dayOfYear;
        }
    }
}
