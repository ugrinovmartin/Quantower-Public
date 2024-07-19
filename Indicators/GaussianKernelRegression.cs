using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace CustomIndicators.KernelIndicators
{
    public sealed class Gaussian_Kernel : Indicator, IWatchlistIndicator
    {
        [InputParameter("Period of Gaussian Kernel", 0, 1, 9999, 1, 0)]
        public int Period = 20;

        [InputParameter("Sources prices for Gaussian Kernel", 1, variants: [
         "Close", PriceType.Close,
         "Open", PriceType.Open,
         "High", PriceType.High,
         "Low", PriceType.Low,
         "Typical", PriceType.Typical,
         "Medium", PriceType.Median,
         "Weighted", PriceType.Weighted,
         "Volume", PriceType.Volume,
         "Open interest", PriceType.OpenInterest
        ])]
        public PriceType SourcePrice = PriceType.Close;

        public int MinHistoryDepths => this.Period;
        public override string ShortName => $"Gaussian Kernel ({this.Period}: {this.SourcePrice})";

        public Gaussian_Kernel()
            : base()
        {
            Name = "Gaussian_Kernel";
            Description = "Gaussian Kernel Indicator";
            AddLineSeries("Gaussian Kernel Line", Color.CadetBlue, 2, LineStyle.Solid);
            SeparateWindow = false;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < Period) return;

            double sum = 0.0;
            double norm = 0.0;

            for (int i = 0; i < Period; i++)
            {
                double price = this.GetPrice(SourcePrice, this.Count - 1 - i);
                double weight = Math.Exp(-Math.Pow(i, 2) / (2 * Math.Pow(Period / 2.0, 2)));
                sum += price * weight;
                norm += weight;
            }

            double gaussianValue = norm != 0 ? sum / norm : 0;
            this.SetValue(gaussianValue);
        }
    }
}
