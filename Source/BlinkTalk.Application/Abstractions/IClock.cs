using System;

namespace BlinkTalk.Application.Abstractions
{
    /// <summary>Abstracts the current time so date-based logic is testable.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public static class DateInt
    {
        /// <summary>
        /// Encodes a date as the integer YYYYMMDD, matching the original
        /// AutoMigratingDatabase.DateToInt used for WordSequences.LastUsedDate.
        /// </summary>
        public static int FromDate(DateTime date) => (date.Year * 10000) + (date.Month * 100) + date.Day;

        public static int Today(IClock clock) => FromDate(clock.UtcNow.Date);
    }
}
