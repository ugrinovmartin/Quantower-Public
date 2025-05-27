using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace KernelSmoothers
{
    public class RationalQuadraticKernelIndicator : Indicator
    {
        [InputParameter("Lookback Period", 0, 1, 999, 1, 0)]
        public int Period = 14;

        [InputParameter("Relative Weight (alpha)", 1, 0.01, 100, 0.01, 0)]
        public float RelativeWeight = 1;

        [InputParameter("Start at Bar", 2, 0, 999, 1, 0)]
        public int StartAtBar = 0;

        [InputParameter("Source Price", 4, variants:
         [
            "Close",
             PriceType.Close,
             "Open",
             PriceType.Open,
             "High",
             PriceType.High,
             "Low",
             PriceType.Low,
             "Typical",
             PriceType.Typical,
             "Medium",
             PriceType.Median,
             "Weighted",
             PriceType.Weighted,
             "Volume",
             PriceType.Volume
         ])]
        public PriceType SourcePrice = PriceType.Close;

        public int MinHistoryDepths => this.Period;

        public RationalQuadraticKernelIndicator() : base()
        {
            this.Name = "Rational Quadratic Kernel Smoother";
            this.Description = "Smooths price data using the Rational Quadratic kernel function.";

            this.AddLineSeries("RQK Line", Color.LightCoral, 2, LineStyle.Solid);
            this.SeparateWindow = false;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < Period + StartAtBar)
                return;

            double currentWeight = 0.0;
            double cumulativeWeight = 0.0;

            for (int i = StartAtBar; i < Period + StartAtBar; i++)
            {
                double y = this.GetPrice(SourcePrice, this.Count - 1 - i);
                double w = Math.Pow(1 + (Math.Pow(i, 2) / (2 * RelativeWeight * Math.Pow(Period, 2))), -RelativeWeight);
                currentWeight += y * w;
                cumulativeWeight += w;
            }

            double yhat = cumulativeWeight != 0 ? currentWeight / cumulativeWeight : double.NaN;
            this.SetValue(yhat);
        }
    }
}
