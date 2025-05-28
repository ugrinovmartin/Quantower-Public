using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace PhysicsIndicators
{
    public class IndicatorMultiscaleVolatility : Indicator, IWatchlistIndicator
    {
        [InputParameter("Short Scale", 0, 2, 100, 1, 0)]
        public int ShortScale = 5;

        [InputParameter("Medium Scale", 1, 5, 200, 1, 0)]
        public int MediumScale = 20;

        [InputParameter("Long Scale", 2, 10, 500, 1, 0)]
        public int LongScale = 60;

        [InputParameter("Sources prices", 3, variants: new object[] {
            "Close", PriceType.Close,
            "Open", PriceType.Open,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Typical", PriceType.Typical
        })]
        public PriceType SourcePrice = PriceType.Close;

        public int MinHistoryDepths => this.LongScale + 1;
        public override string ShortName => $"MSV ({this.ShortScale}:{this.MediumScale}:{this.LongScale})";

        public IndicatorMultiscaleVolatility() : base()
        {
            this.Name = "Multiscale Volatility";
            this.Description = "Volatility across multiple time scales";

            this.AddLineSeries("Short Vol", Color.Red, 1, LineStyle.Solid);
            this.AddLineSeries("Medium Vol", Color.Orange, 1, LineStyle.Solid);
            this.AddLineSeries("Long Vol", Color.Blue, 1, LineStyle.Solid);
            this.AddLineSeries("Vol Ratio", Color.Purple, 2, LineStyle.Solid);

            this.SeparateWindow = true;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < this.MinHistoryDepths)
                return;

            double shortVol = CalculateVolatility(this.ShortScale);
            double mediumVol = CalculateVolatility(this.MediumScale);
            double longVol = CalculateVolatility(this.LongScale);

            this.SetValue(shortVol, 0);
            this.SetValue(mediumVol, 1);
            this.SetValue(longVol, 2);

            // Volatility ratio (short/long) - signals regime changes
            double volRatio = longVol > 0 ? shortVol / longVol : 1.0;
            this.SetValue(volRatio, 3);

            // Color coding for volatility buildup
            if (volRatio > 2.0)
                this.LinesSeries[3].SetMarker(0, Color.Red);    // High volatility regime
            else if (volRatio < 0.5)
                this.LinesSeries[3].SetMarker(0, Color.Green);  // Low volatility regime
            else
                this.LinesSeries[3].SetMarker(0, Color.Gray);
        }

        private double CalculateVolatility(int period)
        {
            if (this.Count < period + 1)
                return 0.0;

            var returns = new List<double>();
            for (int i = 1; i < period + 1; i++)
            {
                double currentPrice = this.GetPrice(this.SourcePrice, i - 1);
                double previousPrice = this.GetPrice(this.SourcePrice, i);
                if (previousPrice != 0)
                {
                    returns.Add(Math.Log(currentPrice / previousPrice));
                }
            }

            if (returns.Count == 0)
                return 0.0;

            double mean = returns.Average();
            double variance = returns.Sum(r => Math.Pow(r - mean, 2)) / returns.Count;
            return Math.Sqrt(variance);
        }
    }
}