using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;

namespace PhysicsIndicators
{
    public class IndicatorQuantumTunneling : Indicator, IWatchlistIndicator
    {
        [InputParameter("Lookback Period", 0, 50, 1000, 1, 0)]
        public int LookbackPeriod = 200;

        [InputParameter("Energy Period", 1, 5, 100, 1, 0)]
        public int EnergyPeriod = 20;

        [InputParameter("Level Sensitivity", 2, 0.1, 5.0, 0.1, 1)]
        public double LevelSensitivity = 1.5;

        [InputParameter("Min Level Touches", 3, 2, 10, 1, 0)]
        public int MinTouches = 3;

        [InputParameter("Barrier Width Multiplier", 4, 0.5, 5.0, 0.1, 1)]
        public double BarrierWidthMultiplier = 2.0;

        [InputParameter("Sources prices", 5, variants: new object[] {
            "Close", PriceType.Close,
            "High", PriceType.High,
            "Low", PriceType.Low,
            "Typical", PriceType.Typical
        })]
        public PriceType SourcePrice = PriceType.Close;

        public int MinHistoryDepths => this.LookbackPeriod;
        public override string ShortName => $"QTunnel ({this.LookbackPeriod})";

        private readonly List<QuantumLevel> resistanceLevels = new List<QuantumLevel>();
        private readonly List<QuantumLevel> supportLevels = new List<QuantumLevel>();
        private Indicator atr;

        public IndicatorQuantumTunneling() : base()
        {
            this.Name = "Quantum Tunneling Probability";
            this.Description = "Calculates probability of breaking through resistance/support levels using quantum tunneling analogy";

            this.AddLineSeries("Tunneling Probability", Color.Purple, 2, LineStyle.Solid);
            this.AddLineSeries("Market Energy", Color.Cyan, 1, LineStyle.Solid);
            this.AddLineSeries("Barrier Strength", Color.Orange, 1, LineStyle.Solid);

            this.AddLineLevel(0.7, "High Probability", Color.Green, 1, LineStyle.Dash);
            this.AddLineLevel(0.3, "Low Probability", Color.Red, 1, LineStyle.Dash);

            this.SeparateWindow = true;
        }

        protected override void OnInit()
        {
            this.atr = Core.Indicators.BuiltIn.ATR(14, MaMode.SMA);
            this.AddIndicator(this.atr);
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < this.MinHistoryDepths)
                return;

            // Refresh sup/res need to improve this
            if (this.Count % 10 == 0)
            {
                this.UpdateKeyLevels();
            }

            double currentPrice = this.GetPrice(this.SourcePrice);
            double marketEnergy = this.CalculateMarketEnergy();

            var nearestLevel = this.FindNearestLevel(currentPrice);

            if (nearestLevel != null)
            {
                double tunnelingProbability = this.CalculateTunnelingProbability(
                    currentPrice, marketEnergy, nearestLevel);

                this.SetValue(tunnelingProbability, 0);
                this.SetValue(marketEnergy, 1);
                this.SetValue(nearestLevel.Strength, 2);

                // Color coding based on probability
                if (tunnelingProbability > 0.7)
                    this.LinesSeries[0].SetMarker(0, Color.Green);   // High breakthrough probability
                else if (tunnelingProbability < 0.3)
                    this.LinesSeries[0].SetMarker(0, Color.Red);     // Low breakthrough probability
                else
                    this.LinesSeries[0].SetMarker(0, Color.Yellow);  // Uncertain
            }
            else
            {
                this.SetValue(0.5, 0);  // Neutral when no clear levels
                this.SetValue(marketEnergy, 1);
                this.SetValue(0, 2);
            }
        }

        private double CalculateMarketEnergy()
        {
            // Market energy = momentum + volume pressure + volatility component
            double momentum = 0;
            double volumeEnergy = 0;
            double volatilityComponent = this.atr.GetValue();

            // Calculate momentum energy
            if (this.Count >= this.EnergyPeriod)
            {
                double priceChange = this.GetPrice(this.SourcePrice) -
                                   this.GetPrice(this.SourcePrice, this.EnergyPeriod);
                momentum = Math.Abs(priceChange) / volatilityComponent;
            }

            // Calculate volume energy (normalized 0-1)
            double avgVolume = 0;
            for (int i = 0; i < Math.Min(this.EnergyPeriod, this.Count); i++)
            {
                avgVolume += this.Volume(i);
            }
            avgVolume /= Math.Min(this.EnergyPeriod, this.Count);

            if (avgVolume > 0)
            {
                volumeEnergy = this.Volume() / avgVolume;
            }

            // Combine components (weighted average)
            double totalEnergy = (momentum * 0.4) + (volumeEnergy * 0.4) + (volatilityComponent * 0.2);

            // Normalize to 0-1 range
            return Math.Min(1.0, totalEnergy / 3.0);
        }

        private void UpdateKeyLevels()
        {
            this.resistanceLevels.Clear();
            this.supportLevels.Clear();

            var pivotHighs = this.FindPivotHighs();
            var pivotLows = this.FindPivotLows();

            // Group nearby pivots into levels
            this.resistanceLevels.AddRange(this.CreateLevelsFromPivots(pivotHighs, true));
            this.supportLevels.AddRange(this.CreateLevelsFromPivots(pivotLows, false));
        }

        private List<PivotPoint> FindPivotHighs()
        {
            var pivots = new List<PivotPoint>();
            int lookback = 5;

            for (int i = lookback; i < Math.Min(this.LookbackPeriod, this.Count - lookback); i++)
            {
                double high = this.High(i);
                bool isPivot = true;

                // Check if this is a local maximum
                for (int j = 1; j <= lookback; j++)
                {
                    if (this.High(i - j) >= high || this.High(i + j) >= high)
                    {
                        isPivot = false;
                        break;
                    }
                }

                if (isPivot)
                {
                    pivots.Add(new PivotPoint
                    {
                        Price = high,
                        Time = this.Time(i),
                        Volume = this.Volume(i),
                        Index = this.Count - i - 1
                    });
                }
            }

            return pivots;
        }

        private List<PivotPoint> FindPivotLows()
        {
            var pivots = new List<PivotPoint>();
            int lookback = 5;

            for (int i = lookback; i < Math.Min(this.LookbackPeriod, this.Count - lookback); i++)
            {
                double low = this.Low(i);
                bool isPivot = true;

                // Check if this is a local minimum
                for (int j = 1; j <= lookback; j++)
                {
                    if (this.Low(i - j) <= low || this.Low(i + j) <= low)
                    {
                        isPivot = false;
                        break;
                    }
                }

                if (isPivot)
                {
                    pivots.Add(new PivotPoint
                    {
                        Price = low,
                        Time = this.Time(i),
                        Volume = this.Volume(i),
                        Index = this.Count - i - 1
                    });
                }
            }

            return pivots;
        }

        private List<QuantumLevel> CreateLevelsFromPivots(List<PivotPoint> pivots, bool isResistance)
        {
            var levels = new List<QuantumLevel>();
            double atrValue = this.atr.GetValue();
            double groupingThreshold = atrValue * this.LevelSensitivity;

            var groupedPivots = new List<List<PivotPoint>>();

            foreach (var pivot in pivots)
            {
                bool addedToGroup = false;

                foreach (var group in groupedPivots)
                {
                    if (Math.Abs(group[0].Price - pivot.Price) <= groupingThreshold)
                    {
                        group.Add(pivot);
                        addedToGroup = true;
                        break;
                    }
                }

                if (!addedToGroup)
                {
                    groupedPivots.Add(new List<PivotPoint> { pivot });
                }
            }

            // Convert groups to levels
            foreach (var group in groupedPivots)
            {
                if (group.Count >= this.MinTouches)
                {
                    double avgPrice = group.Average(p => p.Price);
                    double totalVolume = group.Sum(p => p.Volume);
                    double priceStdDev = Math.Sqrt(group.Sum(p => Math.Pow(p.Price - avgPrice, 2)) / group.Count);

                    // Strength based on touches, volume, and recency
                    double strength = Math.Log(group.Count + 1) * Math.Log(totalVolume + 1) *
                                    (1.0 / (1.0 + priceStdDev / atrValue));

                    levels.Add(new QuantumLevel
                    {
                        Price = avgPrice,
                        Strength = Math.Min(1.0, strength / 10.0), // Normalize
                        Width = Math.Max(priceStdDev, atrValue * 0.5),
                        TouchCount = group.Count,
                        IsResistance = isResistance,
                        LastTouch = group.Max(p => p.Index)
                    });
                }
            }

            return levels;
        }

        private QuantumLevel FindNearestLevel(double currentPrice)
        {
            var allLevels = new List<QuantumLevel>();
            allLevels.AddRange(this.resistanceLevels);
            allLevels.AddRange(this.supportLevels);

            if (allLevels.Count == 0)
                return null;

            return allLevels.OrderBy(l => Math.Abs(l.Price - currentPrice)).First();
        }

        private double CalculateTunnelingProbability(double currentPrice, double marketEnergy, QuantumLevel level)
        {
            // Quantum tunneling probability: P = exp(-2 * κ * a)
            // where κ = sqrt(2m(V-E))/h

            double barrierHeight = level.Strength;  // V (0-1 normalized)
            double particleEnergy = marketEnergy;   // E (0-1 normalized)
            double barrierWidth = level.Width / this.atr.GetValue() * this.BarrierWidthMultiplier;  // a (normalized)

            // Market mass (inertia) - higher volume and volatility = lower mass (easier to move)
            double marketMass = 1.0 / (1.0 + marketEnergy);

            // Energy difference (if negative, we're already above barrier)
            double energyDifference = barrierHeight - particleEnergy;

            if (energyDifference <= 0)
            {
                // Classical breakthrough (particle has enough energy)
                return 0.95;
            }

            // Quantum parameter κ (simplified, units cancelled out)
            double kappa = Math.Sqrt(2 * marketMass * energyDifference);

            // Tunneling probability
            double tunnelingProb = Math.Exp(-2 * kappa * barrierWidth);

            // Distance factor - closer to level = higher probability
            double distanceFactor = 1.0 / (1.0 + Math.Abs(currentPrice - level.Price) / level.Width);

            // Final probability with distance weighting
            double finalProbability = tunnelingProb * distanceFactor;

            return Math.Min(0.99, Math.Max(0.01, finalProbability));
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            var gr = args.Graphics;
            var converter = this.CurrentChart.MainWindow.CoordinatesConverter;

            foreach (var level in this.resistanceLevels)
            {
                this.DrawLevel(gr, converter, level, Color.Red, args.Rectangle);
            }

            foreach (var level in this.supportLevels)
            {
                this.DrawLevel(gr, converter, level, Color.Green, args.Rectangle);
            }
        }

        private void DrawLevel(Graphics gr, IChartWindowCoordinatesConverter converter,
                              QuantumLevel level, Color color, Rectangle rect)
        {
            float y = (float)converter.GetChartY(level.Price);

            if (y < rect.Top || y > rect.Bottom)
                return;

            using (var pen = new Pen(Color.FromArgb(100, color), 2))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                gr.DrawLine(pen, rect.Left, y, rect.Right, y);
            }

            using (var font = new Font("Arial", 8))
            {
                string strengthText = $"{level.Strength:F2}";
                gr.DrawString(strengthText, font, new SolidBrush(color), rect.Right - 50, y - 10);
            }
        }
    }

    public class PivotPoint
    {
        public double Price { get; set; }
        public DateTime Time { get; set; }
        public double Volume { get; set; }
        public int Index { get; set; }
    }

    public class QuantumLevel
    {
        public double Price { get; set; }
        public double Strength { get; set; }
        public double Width { get; set; }
        public int TouchCount { get; set; }
        public bool IsResistance { get; set; }
        public int LastTouch { get; set; }
    }
}