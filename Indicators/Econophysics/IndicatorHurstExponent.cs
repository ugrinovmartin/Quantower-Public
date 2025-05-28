using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace PhysicsIndicators
{
    public class IndicatorHurstExponent : Indicator, IWatchlistIndicator
    {
        [InputParameter("Window Period", 0, 20, 500, 1, 0)]
        public int WindowPeriod = 100;

        [InputParameter("Max Lag", 1, 2, 50, 1, 0)]
        public int MaxLag = 20;

        [InputParameter("Sources prices", 2, variants: new object[] {
            "Close", PriceType.Close,
            "Open", PriceType.Open,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Typical", PriceType.Typical,
            "Median", PriceType.Median,
            "Weighted", PriceType.Weighted
        })]
        public PriceType SourcePrice = PriceType.Close;

        public int MinHistoryDepths => this.WindowPeriod + this.MaxLag;
        public override string ShortName => $"Hurst ({this.WindowPeriod})";

        public IndicatorHurstExponent() : base()
        {
            this.Name = "Hurst Exponent";
            this.Description = "Measures persistence or anti-persistence in price movements";

            this.AddLineSeries("Hurst", Color.DodgerBlue, 2, LineStyle.Solid);
            this.AddLineLevel(0.5, "Random Walk", Color.Gray, 1, LineStyle.Dash);
            this.AddLineLevel(0.6, "Trending", Color.Green, 1, LineStyle.Dot);
            this.AddLineLevel(0.4, "Mean Reverting", Color.Red, 1, LineStyle.Dot);

            this.SeparateWindow = true;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < this.MinHistoryDepths)
                return;

            double hurstValue = CalculateHurstExponent();
            this.SetValue(hurstValue);

            // Color coding based on regime
            if (hurstValue > 0.55)
                this.LinesSeries[0].SetMarker(0, Color.Green);  // Trending
            else if (hurstValue < 0.45)
                this.LinesSeries[0].SetMarker(0, Color.Red);    // Mean reverting
            else
                this.LinesSeries[0].SetMarker(0, Color.Gray);   // Random
        }

        private double CalculateHurstExponent()
        {
            var prices = new List<double>();
            for (int i = 0; i < this.WindowPeriod; i++)
            {
                prices.Add(this.GetPrice(this.SourcePrice, i));
            }

            var logLags = new List<double>();
            var logTau = new List<double>();

            for (int lag = 2; lag <= Math.Min(this.MaxLag, this.WindowPeriod / 2); lag++)
            {
                var differences = new List<double>();
                for (int i = 0; i < prices.Count - lag; i++)
                {
                    differences.Add(prices[i] - prices[i + lag]);
                }

                if (differences.Count > 0)
                {
                    double variance = differences.Sum(x => x * x) / differences.Count;
                    double tau = Math.Sqrt(variance);

                    if (tau > 0)
                    {
                        logLags.Add(Math.Log(lag));
                        logTau.Add(Math.Log(tau));
                    }
                }
            }

            if (logLags.Count < 3)
                return 0.5;

            // Linear regression to find slope (Hurst exponent)
            double n = logLags.Count;
            double sumX = logLags.Sum();
            double sumY = logTau.Sum();
            double sumXY = logLags.Zip(logTau, (x, y) => x * y).Sum();
            double sumX2 = logLags.Sum(x => x * x);

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return Math.Max(0.1, Math.Min(0.9, slope)); // Clamp between 0.1 and 0.9
        }
    }
}