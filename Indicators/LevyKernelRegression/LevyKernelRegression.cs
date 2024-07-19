using TradingPlatform.BusinessLayer;
using System;
using System.Drawing;

namespace CustomIndicators.KernelIndicators
{
    public sealed class LevyKernelIndicator : Indicator, IWatchlistIndicator
    {
        [InputParameter("Period of Levy Kernel", 0, 1, 9999, 1, 0)]
        public int Period = 20;

        [InputParameter("Mu (location parameter)", 1, -9999, 9999, 0.01, 0)]
        public int Mu = 0;

        [InputParameter("C (scale parameter)", 2, 0.0001, 9999, 0.01, 0)]
        public int C = 1;

        [InputParameter("Start at Bar", 3, 1, 9999, 1, 0)]
        public int StartAtBar = 25;

        [InputParameter("Source Price", 4, variants:
         [
            "Close", PriceType.Close,
            "Open", PriceType.Open,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Typical", PriceType.Typical,
            "Medium", PriceType.Median,
            "Weighted", PriceType.Weighted,
            "Volume", PriceType.Volume
         ])]

        public PriceType SourcePrice = PriceType.Close;
        public int MinHistoryDepths => this.Period;

        public LevyKernelIndicator() : base()
        {
            this.Name = "Levy Kernel";
            this.Description = "Applies the Levy Kernel to price data.";
            this.AddLineSeries("Levy Kernel Line", Color.Cyan, 1, LineStyle.Solid);
            this.SeparateWindow = false;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < Period + StartAtBar)
                return;

            double numerator = 0.0;
            double denominator = 0.0;

            for (int i = this.Count - Period; i < this.Count; i++)
            {
                double y = this.GetPrice(SourcePrice, i);
                double x = i - (this.Count - Period); 
                double w;

                if (x > Mu)
                {
                    w = Math.Sqrt(C / (2 * Math.PI)) * Math.Exp(-C / (2 * (x - Mu))) / Math.Pow((x - Mu), 1.5);
                    numerator += y * w;
                    denominator += w;
                }
            }

            double levyValue = denominator != 0 ? numerator / denominator : double.NaN;
            this.SetValue(levyValue);
        }
    }
}