using System;

namespace EconomicEventsIndicator
{
    public enum EventStatus
    {
        Upcoming,
        Pending,
        Completed
    }
    public class ForexEvent
    {
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Currency { get; set; }
        public string Event { get; set; }
        public string Impact { get; set; }
        public string Result { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Upcoming;
        public DateTime EventDateTime =>
            Time.Equals("All Day", StringComparison.OrdinalIgnoreCase)
                ? Date
                : DateTime.TryParseExact(Time, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime parsedTime)
                    ? Date.Date.Add(parsedTime.TimeOfDay)
                    : Date;
        public string GetDisplayStatus()
        {
            if (Time.Equals("All Day", StringComparison.OrdinalIgnoreCase))
                return "All Day";

            // If event has a result, show it
            if (!string.IsNullOrEmpty(Result))
                return Result;

            return "Pending";
        }
    }
}