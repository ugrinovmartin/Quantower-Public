using System;

namespace EconomicEventsIndicator
{
    public class ForexEvent
    {
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Currency { get; set; }
        public string Event { get; set; }
        public string Impact { get; set; }
    }
}
