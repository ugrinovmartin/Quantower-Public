using System;
using System.Text.RegularExpressions;
using TradingPlatform.BusinessLayer;

namespace EconomicEventsIndicator
{
    public static class TimeZoneHelper
    {
        public static double GetOffsetHours(TradingPlatform.BusinessLayer.TimeZone chartTimeZone)
        {
            string timeZoneString = chartTimeZone.ToString();
            var match = Regex.Match(timeZoneString, @"\(UTC([+-])(\d{2}):(\d{2})\)");

            if (match.Success)
            {
                string sign = match.Groups[1].Value;
                int hours = int.Parse(match.Groups[2].Value);
                int minutes = int.Parse(match.Groups[3].Value);

                double offset = hours + (minutes / 60.0);
                return sign == "+" ? offset : -offset;
            }

            return 0;
        }

        public static ForexEvent AdjustEventTime(ForexEvent forexEvent, double chartOffsetHours)
        {
            if (forexEvent.Time.Equals("All Day", StringComparison.OrdinalIgnoreCase))
                return forexEvent;

            try
            {
                bool parseSuccess = DateTime.TryParseExact(
                    forexEvent.Time,
                    "h:mmtt",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime parsedTime);

                if (!parseSuccess)
                {
                    return forexEvent;
                }

                double localOffset = TimeZoneInfo.Local.BaseUtcOffset.TotalHours;

                var utcTime = parsedTime.AddHours(-localOffset);

                var targetTime = utcTime.AddHours(chartOffsetHours);

                forexEvent.Time = targetTime.ToString("h:mmtt");

                if (targetTime.Day != parsedTime.Day)
                {
                    if (targetTime < parsedTime)
                    {
                        forexEvent.Date = forexEvent.Date.AddDays(-1);
                    }
                    else
                    {
                        forexEvent.Date = forexEvent.Date.AddDays(1);
                    }
                }

                return forexEvent;
            }
            catch (Exception ex)
            {
                return forexEvent;
            }
        }
    }
}