using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;

namespace EconomicEventsIndicator
{
    public partial class EconomicEventsIndicator : Indicator
    {
        public int dateMode = 1;
        public DateTime customStartDate = DateTime.UtcNow.Date;
        public DateTime customEndDate = DateTime.UtcNow.Date;
        public bool highImpact = true;
        public bool mediumImpact = true;
        public bool lowImpact = true;
        public bool nonEconomicNews = true;
        public int currencyMode = 1;
        public int timeZoneMode = 1;

        public bool audSelected;
        public bool cadSelected;
        public bool chfSelected;
        public bool cnySelected;
        public bool eurSelected;
        public bool gbpSelected;
        public bool jpySelected;
        public bool nzdSelected;
        public bool usdSelected;

        public List<ForexEvent> forexEvents;
        public Font font;
        private readonly object lockObject = new object();

        public int newsPositionX = 300;
        public int newsPositionY = 10;


        public EconomicEventsIndicator()
        : base()
        {
            Name = "Economic Events Indicator";
            Description = "Display Economic Events";
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            string[] fontNames = { "Droid Sans Mono", "DejaVu Sans Mono", "Consolas", "Verdana" };
            foreach (var fontName in fontNames)
            {
                font = new Font(fontName, 10);
                if (font.Name == fontName)
                {
                    break;
                }
            }

            Task.Run(() => FetchAndFilterData());
        }
        protected override void OnUpdate(UpdateArgs args)
        {
            TradingPlatform.BusinessLayer.TimeZone chartTimeZone = CurrentChart.CurrentTimeZone;
        }

        private async Task FetchAndFilterData()
        {
            DateTime currentDate = this.Symbol.LastDateTime.Date;

            List<ForexEvent> events = new List<ForexEvent>();

            if (dateMode == 1) 
            {
                string date = currentDate.ToString("MMMdd.yyyy").ToLower();
                string url = $"https://www.forexfactory.com/calendar?day={date}";
                events = await ForexFactoryScraper.GetForexFactoryEvents(url);
            }
            else if (dateMode == 2 && customStartDate != DateTime.MinValue && customEndDate != DateTime.MinValue) 
            {
                DateTime startDate = customStartDate;
                DateTime endDate = customEndDate;

                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    string dateStr = date.ToString("MMMdd.yyyy").ToLower();
                    string url = $"https://www.forexfactory.com/calendar?day={dateStr}";
                    var dayEvents = await ForexFactoryScraper.GetForexFactoryEvents(url);
                    foreach (var e in dayEvents)
                    {
                        e.Date = date;
                    }
                    events.AddRange(dayEvents);
                }
            }

            double offsetHours;
            if (timeZoneMode == 1)
            {
                //Core.Instance.Loggers.Log($"Using Chart TimeZone: {CurrentChart.CurrentTimeZone}"); 
                offsetHours = TimeZoneHelper.GetOffsetHours(CurrentChart.CurrentTimeZone);
            }
            else
            {
                offsetHours = TimeZoneInfo.Local.BaseUtcOffset.TotalHours;
            }

            //Core.Instance.Loggers.Log($"Offset Hours: {offsetHours}");

            lock (lockObject)
            {
                forexEvents = events.Select(e => TimeZoneHelper.AdjustEventTime(e, offsetHours))
                                   .Where(ShouldIncludeEvent)
                                   .ToList();
            }

            this.CurrentChart.Refresh();
        }

        private bool ShouldIncludeEvent(ForexEvent forexEvent)
        {
            if (currencyMode == 2)
            {
                var selectedCurrencies = new List<string>();
                if (audSelected) selectedCurrencies.Add("AUD");
                if (cadSelected) selectedCurrencies.Add("CAD");
                if (chfSelected) selectedCurrencies.Add("CHF");
                if (cnySelected) selectedCurrencies.Add("CNY");
                if (eurSelected) selectedCurrencies.Add("EUR");
                if (gbpSelected) selectedCurrencies.Add("GBP");
                if (jpySelected) selectedCurrencies.Add("JPY");
                if (nzdSelected) selectedCurrencies.Add("NZD");
                if (usdSelected) selectedCurrencies.Add("USD");

                if (!selectedCurrencies.Contains(forexEvent.Currency))
                {
                    return false;
                }
            }

            if ((forexEvent.Impact == "High Impact Expected" && !highImpact) ||
                (forexEvent.Impact == "Medium Impact Expected" && !mediumImpact) ||
                (forexEvent.Impact == "Low Impact Expected" && !lowImpact) ||
                (forexEvent.Impact == "Non-Economic" && !nonEconomicNews))
            {
                return false;
            }

            return true;
        }
        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (this.CurrentChart == null || this.Symbol == null || forexEvents == null)
            {
                return;
            }

            var gr = args.Graphics;
            var rectangle = this.CurrentChart.Windows[args.WindowIndex].ClientRectangle;
            gr.SetClip(rectangle);

            int x = rectangle.Left + newsPositionX;
            int y = rectangle.Top + newsPositionY;

            string header = $"{DateTime.Now:dd/MM/yy} News via Forex Factory || By @ugrinovv";
            var headerBrush = Brushes.Cyan;
            var headerFont = new Font(font.FontFamily, 12, FontStyle.Bold);

            gr.DrawString(header, headerFont, headerBrush, x + 2, y + 2);
            y += headerFont.Height + 6;

            lock (lockObject)
            {
                var sortedEvents = forexEvents.OrderBy(e => e.Date).ThenBy(e => ParseEventDateTimeForSorting(e.Time)).ToList();
                DateTime? lastDate = null;

                foreach (var forexEvent in sortedEvents)
                {
                    if (dateMode == 2 && (lastDate == null || forexEvent.Date != lastDate))
                    {
                        lastDate = forexEvent.Date;
                        string dateHeader = $"{forexEvent.Date:dd/MM/yyyy} ({forexEvent.Date:dddd})";
                        gr.DrawString(dateHeader, headerFont, Brushes.White, x + 2, y + 2);
                        y += headerFont.Height + 6;
                    }

                    var impactBrush = forexEvent.Impact switch
                    {
                        "High Impact Expected" => Brushes.Red,
                        "Medium Impact Expected" => Brushes.Yellow,
                        "Low Impact Expected" => Brushes.Green,
                        "Non-Economic" => Brushes.White,
                        _ => Brushes.White
                    };

                    gr.DrawString($"{forexEvent.Time} {forexEvent.Currency}", font, Brushes.White, x + 2, y + 2);

                    gr.DrawString($"{forexEvent.Event}", font, impactBrush, x + 100, y + 2);

                    string displayValue;
                    Brush displayBrush;

                    if (!string.IsNullOrEmpty(forexEvent.Result))
                    {
                        displayValue = forexEvent.Result;
                        displayBrush = Brushes.Green;
                    }
                    else if (forexEvent.Time.Equals("All Day", StringComparison.OrdinalIgnoreCase))
                    {
                        displayValue = "All Day";
                        displayBrush = Brushes.Cyan;
                    }
                    else
                    {
                        displayValue = "Pending";
                        displayBrush = Brushes.Yellow;
                    }

                    gr.DrawString(displayValue, font, displayBrush, x + 400, y + 2);

                    y += font.Height + 6;
                }
            }
        }
        private DateTime ParseEventDateTimeForSorting(string timeString)
        {
            DateTime baseDate = DateTime.Today;

            if (timeString.Equals("All Day", StringComparison.OrdinalIgnoreCase))
            {
                return baseDate;
            }

            if (DateTime.TryParseExact(timeString, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime parsedTime))
            {
                return baseDate.AddHours(parsedTime.Hour).AddMinutes(parsedTime.Minute);
            }

            System.Diagnostics.Debug.WriteLine("Failed to parse time: " + timeString);
            return baseDate;
        }

        public override void Dispose()
        {
            font?.Dispose();
            base.Dispose();
        }

        private void Refresh()
        {
            Task.Run(() => FetchAndFilterData());
        }
    }
}