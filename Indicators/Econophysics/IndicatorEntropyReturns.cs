using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace PhysicsIndicators
{
    public class IndicatorEntropyReturns : Indicator, IWatchlistIndicator
    {
        [InputParameter("Period", 0, 10, 500, 1, 0)]
        public int Period = 100;

        [InputParameter("Bins", 1, 5, 50, 1, 0)]
        public int Bins = 10;

        [InputParameter("Sources prices", 2, variants: new object[] {
            "Close", PriceType.Close,
            "Open", PriceType.Open,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Typical", PriceType.Typical
        })]
        public PriceType SourcePrice = PriceType.Close;

        public int MinHistoryDepths => this.Period + 1;
        public override string ShortName => $"Entropy ({this.Period})";

        public IndicatorEntropyReturns() : base()
        {
            this.Name = "Returns Entropy";
            this.Description = "Shannon entropy of price returns - measures market disorder";

            this.AddLineSeries("Entropy", Color.Purple, 2, LineStyle.Solid);
            this.AddLineSeries("Smoothed", Color.Orange, 1, LineStyle.Solid);

            this.SeparateWindow = true;
        }

        private readonly List<double> entropyValues = new List<double>();

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < this.MinHistoryDepths)
                return;

            double entropy = CalculateReturnsEntropy();
            this.SetValue(entropy, 0);

            // Keep history for smoothing
            entropyValues.Add(entropy);
            if (entropyValues.Count > 20)
                entropyValues.RemoveAt(0);

            // Smoothed entropy
            double smoothedEntropy = entropyValues.Average();
            this.SetValue(smoothedEntropy, 1);

            // Color coding based on entropy level
            if (entropy > smoothedEntropy * 1.2)
                this.LinesSeries[0].SetMarker(0, Color.Red);     // High disorder
            else if (entropy < smoothedEntropy * 0.8)
                this.LinesSeries[0].SetMarker(0, Color.Green);   // Low disorder (more predictable)
            else
                this.LinesSeries[0].SetMarker(0, Color.Gray);
        }

        private double CalculateReturnsEntropy()
        {
            var returns = new List<double>();

            for (int i = 1; i < this.Period + 1; i++)
            {
                double currentPrice = this.GetPrice(this.SourcePrice, i - 1);
                double previousPrice = this.GetPrice(this.SourcePrice, i);

                if (previousPrice != 0 && currentPrice != 0)
                {
                    returns.Add(Math.Log(currentPrice / previousPrice));
                }
            }

            if (returns.Count < 2)
                return 0.0;

            // Create histogram
            double minReturn = returns.Min();
            double maxReturn = returns.Max();
            double range = maxReturn - minReturn;

            if (range == 0)
                return 0.0;

            var histogram = new int[this.Bins];
            double binWidth = range / this.Bins;

            foreach (double ret in returns)
            {
                int binIndex = (int)Math.Floor((ret - minReturn) / binWidth);
                binIndex = Math.Max(0, Math.Min(this.Bins - 1, binIndex));
                histogram[binIndex]++;
            }

            // Calculate Shannon entropy
            double entropy = 0.0;
            int totalReturns = returns.Count;

            for (int i = 0; i < this.Bins; i++)
            {
                if (histogram[i] > 0)
                {
                    double probability = (double)histogram[i] / totalReturns;
                    entropy -= probability * Math.Log(probability) / Math.Log(2);
                }
            }

            return entropy;
        }
    }
}