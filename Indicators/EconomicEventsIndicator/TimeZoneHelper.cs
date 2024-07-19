using System;
using System.Text.RegularExpressions;

namespace EconomicEventsIndicator
{
    public static class TimeZoneHelper
    {
        public static TimeZoneInfo GetTimeZoneFromChart(string timeZoneString)
        {
            var match = Regex.Match(timeZoneString, @"\(UTC([+-]\d{2}:\d{2})\)");
            if (match.Success)
            {
                string offset = match.Groups[1].Value;
                return TimeZoneInfo.CreateCustomTimeZone("Custom TimeZone", TimeSpan.Parse(offset), timeZoneString, timeZoneString);
            }
            return TimeZoneInfo.Local;
        }

        public static ForexEvent AdjustEventTime(ForexEvent forexEvent, TimeZoneInfo timeZone)
        {
            if (DateTime.TryParse(forexEvent.Time, out DateTime eventTime))
            {
                DateTime adjustedTime = TimeZoneInfo.ConvertTime(eventTime, TimeZoneInfo.Utc, timeZone);
                forexEvent.Time = adjustedTime.ToString("HH:mm");
            }
            return forexEvent;
        }
    }
}
