using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace CustomIndicators.KernelIndicators
{
    public class PeriodicKernelIndicator : Indicator
    {
        [InputParameter("Lookback Period", 0, 1, 999, 1, 0)]
        public int LookbackPeriod = 14;

        [InputParameter("Period", 1, 1, 999, 1, 0)]
        public int Period = 14;

        [InputParameter("Start at Bar", 2, 0, 999, 1, 0)]
        public int StartAtBar = 0;

        [InputParameter("Source Price", 3, variants:
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

        public PeriodicKernelIndicator() : base()
        {
            this.Name = "Periodic Kernel";
            this.Description = "An indicator that implements the Periodic Kernel.";

            this.AddLineSeries("PK Line", Color.LightGreen, 2, LineStyle.Solid);
            this.SeparateWindow = false;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < LookbackPeriod + StartAtBar)
                return;

            double currentWeight = 0.0;
            double cumulativeWeight = 0.0;

            for (int i = 0; i < LookbackPeriod; i++)
            {
                double y = this.GetPrice(SourcePrice, this.Count - 1 - i);
                double sinPart = Math.Sin(Math.PI * i / Period);
                double w = Math.Exp(-2 * Math.Pow(sinPart, 2) / Math.Pow(LookbackPeriod, 2));
                currentWeight += y * w;
                cumulativeWeight += w;
            }

            double yhat = cumulativeWeight != 0 ? currentWeight / cumulativeWeight : double.NaN;
            this.SetValue(yhat);
        }
    }
}
