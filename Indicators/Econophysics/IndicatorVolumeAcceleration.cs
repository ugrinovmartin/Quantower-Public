using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace PhysicsIndicators
{
    public class IndicatorVolumeAcceleration : Indicator, IWatchlistIndicator
    {
        [InputParameter("Smoothing Period", 0, 1, 50, 1, 0)]
        public int SmoothingPeriod = 5;

        [InputParameter("Threshold Multiplier", 1, 0.1, 10.0, 0.1, 1)]
        public double ThresholdMultiplier = 2.0;

        public int MinHistoryDepths => Math.Max(3, this.SmoothingPeriod);
        public override string ShortName => $"VolAccel ({this.SmoothingPeriod})";

        private readonly List<double> accelerationHistory = new List<double>();

        public IndicatorVolumeAcceleration() : base()
        {
            this.Name = "Volume Acceleration";
            this.Description = "Second derivative of volume - detects sudden volume changes";

            this.AddLineSeries("Acceleration", Color.Purple, 2, LineStyle.Solid);
            this.AddLineSeries("Smoothed", Color.Orange, 1, LineStyle.Solid);
            this.AddLineSeries("Volume", Color.Gray, 1, LineStyle.Histogramm);
            this.AddLineLevel(0, "Zero", Color.Gray, 1, LineStyle.Dash);

            this.SeparateWindow = true;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < 3)
                return;

            double acceleration = CalculateVolumeAcceleration();
            this.SetValue(acceleration, 0);

            // Volume for reference
            this.SetValue(this.Volume(), 2);

            // Keep history for smoothing and threshold calculation
            accelerationHistory.Add(acceleration);
            if (accelerationHistory.Count > 50) // Keep more history for better statistics
                accelerationHistory.RemoveAt(0);

            // Smoothed acceleration
            if (accelerationHistory.Count >= this.SmoothingPeriod)
            {
                double sum = 0;
                for (int i = accelerationHistory.Count - this.SmoothingPeriod; i < accelerationHistory.Count; i++)
                    sum += accelerationHistory[i];

                double smoothedAcceleration = sum / this.SmoothingPeriod;
                this.SetValue(smoothedAcceleration, 1);

                // Calculate dynamic threshold
                if (accelerationHistory.Count > 10)
                {
                    double sumSquares = 0;
                    double mean = 0;
                    foreach (double val in accelerationHistory)
                        mean += val;
                    mean /= accelerationHistory.Count;

                    foreach (double val in accelerationHistory)
                        sumSquares += Math.Pow(val - mean, 2);

                    double stdDev = Math.Sqrt(sumSquares / accelerationHistory.Count);
                    double threshold = stdDev * this.ThresholdMultiplier;

                    // Color coding based on threshold
                    if (Math.Abs(smoothedAcceleration) > threshold)
                    {
                        if (smoothedAcceleration > 0)
                            this.LinesSeries[1].SetMarker(0, Color.Green);   // Positive surge
                        else
                            this.LinesSeries[1].SetMarker(0, Color.Red);     // Negative surge
                    }
                    else
                    {
                        this.LinesSeries[1].SetMarker(0, Color.Gray);
                    }
                }
            }
        }

        private double CalculateVolumeAcceleration()
        {
            if (this.Count < 3)
                return 0.0;

            double currentVolume = this.Volume(0);
            double previousVolume = this.Volume(1);
            double previousPreviousVolume = this.Volume(2);

            // First derivative (velocity)
            double currentVelocity = currentVolume - previousVolume;
            double previousVelocity = previousVolume - previousPreviousVolume;

            // Second derivative (acceleration)
            double acceleration = currentVelocity - previousVelocity;

            return acceleration;
        }
    }
}