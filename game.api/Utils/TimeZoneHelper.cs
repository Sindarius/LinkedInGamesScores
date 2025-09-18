using System;

namespace game.api.Utils
{
    public static class TimeZoneHelper
    {
        public static TimeZoneInfo GetPacificTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"); }
        }

        // Returns UTC start/end for the Pacific calendar day containing `date` (or today if null), and the Pacific date label
        public static (DateTime utcStart, DateTime utcEnd, DateTime pacificDate) GetPacificDayRange(DateTime? date = null)
        {
            var tz = GetPacificTimeZone();
            DateTime pacificDate;
            if (date.HasValue)
            {
                pacificDate = date.Value.Date; // interpret input as a Pacific date (no time)
            }
            else
            {
                var nowPacific = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                pacificDate = nowPacific.Date;
            }

            var startLocal = DateTime.SpecifyKind(pacificDate, DateTimeKind.Unspecified);
            var endLocal = DateTime.SpecifyKind(pacificDate.AddDays(1), DateTimeKind.Unspecified);
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);
            return (utcStart, utcEnd, pacificDate);
        }

        // Returns UTC window covering the last `days` Pacific calendar days (inclusive of today), with labels and index by Pacific date
        public static (DateTime utcStart, DateTime utcEnd, List<DateTime> pacificDays, Dictionary<DateTime, int> index)
            GetRecentPacificWindows(int days)
        {
            if (days < 1) throw new ArgumentOutOfRangeException(nameof(days));
            var tz = GetPacificTimeZone();
            var nowPacific = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var endDayPacific = nowPacific.Date; // today in Pacific
            var startDayPacific = endDayPacific.AddDays(-(days - 1));

            var pacificDays = new List<DateTime>(days);
            var index = new Dictionary<DateTime, int>();
            for (int i = 0; i < days; i++)
            {
                var d = startDayPacific.AddDays(i);
                pacificDays.Add(d);
                index[d] = i;
            }

            var startLocal = DateTime.SpecifyKind(startDayPacific, DateTimeKind.Unspecified);
            var endLocal = DateTime.SpecifyKind(endDayPacific.AddDays(1), DateTimeKind.Unspecified);
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);

            return (utcStart, utcEnd, pacificDays, index);
        }
    }
}

