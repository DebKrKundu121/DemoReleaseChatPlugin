namespace DoverPort.Common.Models
{
    /// <summary>
    /// A settable clock. Note that the Releases entities call <c>DateTime.Now</c> directly rather
    /// than going through here - that is what the original code does, and changing it would move
    /// every timestamp in the docs and tests.
    /// </summary>
    public static class SystemClock
    {
        private static DateTime? _customDate;

        public static DateTime Now => _customDate ?? DateTime.Now;
        public static DateTime Today => (_customDate ?? DateTime.Now).Date;

        public static DateTime FirstDayOfCurrentYear => new(Now.Year, 1, 1);
        public static DateTime FirstDayOfNextYear => new(Now.Year + 1, 1, 1);
        public static DateTime LastDayOfCurrentYear => FirstDayOfNextYear.AddDays(-1);
        public static DateTime LastDayOfNextYear => new DateTime(Now.Year + 2, 1, 1).AddDays(-1);

        public static void Set(DateTime customDate) => _customDate = customDate;
        public static void Reset() => _customDate = null;

        public static IEnumerable<DateTime> GenerateDates(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
                throw new ArgumentException($"{nameof(endDate)} must be greater than or equal to {nameof(startDate)}");

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
                yield return d;
        }
    }
}
