using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace saurabh_indicator
{
    public class SaurabhIndicator : Indicator
    {
        // Input Parameters
        [InputParameter("Fast EMA Length", 1, minimum: 1, maximum: 300, increment: 1)]
        public int FastEmaLength { get; set; } = 10;

        [InputParameter("Slow EMA Length", 2, minimum: 1, maximum: 300, increment: 1)]
        public int SlowEmaLength { get; set; } = 20;

        [InputParameter("ADX Length", 3, minimum: 1, maximum: 50, increment: 1)]
        public int AdxLength { get; set; } = 10;

        [InputParameter("DI Length", 4, minimum: 1, maximum: 50, increment: 1)]
        public int DiLength { get; set; } = 10;

        [InputParameter("RSI Length", 5, minimum: 1, maximum: 50, increment: 1)]
        public int RsiLength { get; set; } = 10;

        [InputParameter("Supertrend ATR Period", 6, minimum: 1, maximum: 999, increment: 1)]
        public int SupertrendAtrPeriod { get; set; } = 10;

        [InputParameter("Supertrend Multiplier", 7, minimum: 0.01, maximum: 10, increment: 0.01, decimalPlaces: 2)]
        public double SupertrendMultiplier { get; set; } = 3;

        [InputParameter("FVG Threshold %", 8, minimum: 0, maximum: 100, increment: 0.1)]
        public double ThresholdPer { get; set; } = 0.2;

        [InputParameter("Start Time (HH:mm)", 9)]
        public string StartTime { get; set; } = "00:01";

        [InputParameter("End Time (HH:mm)", 10)]
        public string StopTime { get; set; } = "23:59";

        [InputParameter("Up trend color", 11)]
        public Color BullColor = Color.Green;

        [InputParameter("Down trend color", 12)]
        public Color BearColor = Color.Red;

        [InputParameter("Show MAs", 13)]
        public bool showMa { get; set; } = true;

        [InputParameter("Show SuperTrend", 13)]
        public bool ShowTrend { get; set; } = true;

        [InputParameter("Show Marker Icon", 13)]
        public bool ShowMarkers { get; set; } = true;

        [InputParameter("Show Trade Background", 14)]
        public bool ShowBG { get; set; } = true;

        private class TradeRecord
        {
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public bool IsLong { get; set; }
        }

        private List<TradeRecord> tradeHistory = new List<TradeRecord>();
        private TradeRecord currentTrade = null;


        private HistoricalData historicalData;
        private DateTime startTimeDateTime;
        private DateTime endTimeDateTime;

        // Indicators
        private Indicator fastMA;
        private Indicator slowMA;
        private Indicator adx;
        private Indicator rsi;
        private Indicator atr;

        // Trade state tracking
        private bool longInTrade = false;
        private bool shortInTrade = false;
        private bool bullFvg = false;
        private bool bearFvg = false;

        private DateTime longTradeStartTime;
        private DateTime shortTradeStartTime;

        private double prevFinalUpperBand = double.NaN;
        private double prevFinalLowerBand = double.NaN;
        private int prevTrend = 0;
        private double finalUpperBand = double.NaN;
        private double finalLowerBand = double.NaN;
        private int currentTrend = 0;

        public SaurabhIndicator() : base()
        {
            Name = "Saurabh Indicator";
            Description = "Combined indicator with FVG, EMAs, VWAP, Supertrend, ADX, and RSI";
            SeparateWindow = false;
            UpdateType = IndicatorUpdateType.OnTick;

            AddLineSeries("High", Color.FromArgb(0, 0, 0, 0));
            AddLineSeries("Low", Color.FromArgb(0, 0, 0, 0));
            AddLineSeries("Slow Ema", Color.Purple, 1, LineStyle.Solid);
            AddLineSeries("Fast Ema", Color.LightSeaGreen, 1, LineStyle.Solid);
            AddLineSeries("RSI", Color.FromArgb(0, 0, 0, 0));
            AddLineSeries("ADX", Color.FromArgb(0, 0, 0, 0));
            AddLineSeries("Supertrend", Color.DodgerBlue, 2, LineStyle.Solid);
        }

        protected override void OnInit()
        {
            base.OnInit();

            Core.Instance.Loggers.Log("Init");
            // Initialize indicators
            fastMA = Core.Indicators.BuiltIn.EMA(FastEmaLength, PriceType.Close);
            slowMA = Core.Indicators.BuiltIn.EMA(SlowEmaLength, PriceType.Close);
            adx = Core.Indicators.BuiltIn.ADX(AdxLength, MaMode.EMA);
            rsi = Core.Indicators.BuiltIn.RSI(RsiLength, PriceType.Close, RSIMode.Exponential, MaMode.EMA, maperiod: 1);
            atr = Core.Indicators.BuiltIn.ATR(SupertrendAtrPeriod, MaMode.SMA);

            AddIndicator(fastMA);
            AddIndicator(slowMA);
            AddIndicator(adx);
            AddIndicator(rsi);
            AddIndicator(atr);

            startTimeDateTime = DateTime.Today.Add(TimeSpan.ParseExact(StartTime, "HH:mm", null));
            endTimeDateTime = DateTime.Today.Add(TimeSpan.ParseExact(StopTime, "HH:mm", null));
        }

        private bool IsWithinTradingWindow()
        {
            return Time() <= startTimeDateTime && Time() >= endTimeDateTime;
        }

        public bool DetectBullishFVG()
        {
            double currentHigh = High(0);
            double currentOpen = Open(0);
            double currentLow = Low(0);

            double previousOpen = Open(1);
            double previousClose = Close(1);

            double candle3Open = Open(2);
            double candle3Low = Low(2);
            double candle3High = High(2);
            double candle3Close = Close(2);

            double candle2BodySize = Math.Abs(previousClose - previousOpen);
            double gapSize = candle2BodySize - (currentOpen - currentLow + (candle3High - candle3Close));

            double thresholdGapSize = candle2BodySize * ThresholdPer;

            return gapSize >= thresholdGapSize && previousClose - previousOpen > 0 && candle3Close - candle3Open > 0;
        }

        public bool DetectBearishFVG()
        {
            double currentHigh = High(0);
            double currentOpen = Open(0);
            double currentLow = Low(0);

            double previousOpen = Open(1);
            double previousClose = Close(1);

            double candle3Open = Open(2);
            double candle3Low = Low(2);
            double candle3High = High(2);
            double candle3Close = Close(2);

            double candle2BodySize = Math.Abs(previousClose - previousOpen);
            double gapSize = candle2BodySize - (currentHigh - previousClose + (candle3Close - candle3Low));

            double thresholdGapSize = candle2BodySize * ThresholdPer;

            return gapSize >= thresholdGapSize && previousClose - previousOpen < 0 && candle3Close - candle3Open < 0;
        }
        private void CalculateSupertrend()
        {
            if (Count < SupertrendAtrPeriod)
                return;

            double atrValue = atr.GetValue();
            double basicUpperBand = (High(0) + Low(0)) / 2 + SupertrendMultiplier * atrValue;
            double basicLowerBand = (High(0) + Low(0)) / 2 - SupertrendMultiplier * atrValue;

            // Initialize on first calculation
            if (double.IsNaN(prevFinalUpperBand))
            {
                prevFinalUpperBand = basicUpperBand;
                prevFinalLowerBand = basicLowerBand;
                prevTrend = 1;
                return;
            }

            // Calculate Final Bands
            finalUpperBand = basicUpperBand;
            finalLowerBand = basicLowerBand;

            if (basicUpperBand < prevFinalUpperBand || Close(1) > prevFinalUpperBand)
                finalUpperBand = basicUpperBand;
            else
                finalUpperBand = prevFinalUpperBand;

            if (basicLowerBand > prevFinalLowerBand || Close(1) < prevFinalLowerBand)
                finalLowerBand = basicLowerBand;
            else
                finalLowerBand = prevFinalLowerBand;

            // Determine Trend
            currentTrend = prevTrend;

            if (prevTrend == 1)
            {
                if (Close() <= finalLowerBand)
                {
                    currentTrend = -1;
                    finalUpperBand = basicUpperBand;
                }
            }
            else if (prevTrend == -1)
            {
                if (Close() >= finalUpperBand)
                {
                    currentTrend = 1;
                    finalLowerBand = basicLowerBand;
                }
            }

            // Set the Supertrend value and color
            if (currentTrend == 1)
            {
                if (ShowTrend) SetValue(finalLowerBand, 6);
                LinesSeries[6].SetMarker(0, BullColor);
            }
            else
            {
                if (ShowTrend) SetValue(finalUpperBand, 6);
                LinesSeries[6].SetMarker(0, BearColor);
            }

            // Store values for next calculation
            prevFinalUpperBand = finalUpperBand;
            prevFinalLowerBand = finalLowerBand;
            prevTrend = currentTrend;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            try
            {
                if (Count < FastEmaLength) return;
                if (IsWithinTradingWindow()) return;

                double currentRsi = rsi.GetValue(0, 0);
                double currentAdx = adx.GetValue(0, 0);
                double currentFastMa = fastMA.GetValue();
                double currentSlowMa = slowMA.GetValue();

                SetValue(High() + 1.5, 0);
                SetValue(Low() - 1.5, 1);

                if (showMa) SetValue(currentFastMa, 2);
                if (showMa) SetValue(currentSlowMa, 3);
                SetValue(currentRsi, 4);
                SetValue(currentAdx, 5);

                // FVG Detection
                bullFvg = DetectBullishFVG();
                bearFvg = DetectBearishFVG();
                CalculateSupertrend();

                // Combined signal conditions
                bool longSignal = bullFvg && currentTrend == 1 &&
                        rsi.GetValue(0, 0) > rsi.GetValue(1, 0) &&
                        rsi.GetValue(0, 0) > 50 &&
                        fastMA.GetValue() > fastMA.GetValue(1) &&
                        Close() >= Open() &&
                        Close() > Close(1) &&
                        adx.GetValue(0, 0) >= 20;

                bool shortSignal = bearFvg && currentTrend == -1 &&
                       rsi.GetValue(0, 0) < rsi.GetValue(1, 0) &&
                       rsi.GetValue(0, 0) < 50 &&
                       fastMA.GetValue() < fastMA.GetValue(1) &&
                       Close() < Open() &&
                       Close() < Close(1) &&
                       adx.GetValue(0, 0) >= 20;

                // Long exit condition
                bool longExit = fastMA.GetValue() < fastMA.GetValue(1);

                // Short exit condition
                bool shortExit = fastMA.GetValue() > fastMA.GetValue(1);

                // Draw markers
                if (ShowMarkers)
                {
                    if (longSignal && !longInTrade)
                    {
                        LinesSeries[1].SetMarker(1, new IndicatorLineMarker(
                            BullColor,
                            bottomIcon: IndicatorLineMarkerIconType.UpArrow
                        ));
                        longInTrade = true;

                        // Record new trade
                        currentTrade = new TradeRecord
                        {
                            StartTime = Symbol.LastDateTime,
                            IsLong = true
                        };
                        tradeHistory.Add(currentTrade);
                    }
                    // Long exit
                    else if (longExit && longInTrade)
                    {
                        LinesSeries[1].SetMarker(1, new IndicatorLineMarker(
                            BearColor,
                            bottomIcon: IndicatorLineMarkerIconType.UpArrow
                        ));
                        longInTrade = false;

                        // Close current trade
                        if (currentTrade != null && currentTrade.IsLong)
                        {
                            currentTrade.EndTime = Symbol.LastDateTime;
                            currentTrade = null;
                        }
                    }

                    // Short entry
                    if (shortSignal && !shortInTrade)
                    {
                        LinesSeries[1].SetMarker(1, new IndicatorLineMarker(
                            BearColor,
                            bottomIcon: IndicatorLineMarkerIconType.UpArrow
                        ));
                        shortInTrade = true;

                        // Record new trade
                        currentTrade = new TradeRecord
                        {
                            StartTime = Symbol.LastDateTime,
                            IsLong = false
                        };
                        tradeHistory.Add(currentTrade);
                    }
                    // Short exit
                    else if (shortExit && shortInTrade)
                    {
                        LinesSeries[1].SetMarker(1, new IndicatorLineMarker(
                            BullColor,
                            bottomIcon: IndicatorLineMarkerIconType.UpArrow
                        ));
                        shortInTrade = false;

                        // Close current trade
                        if (currentTrade != null && !currentTrade.IsLong)
                        {
                            currentTrade.EndTime = Symbol.LastDateTime;
                            currentTrade = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Loggers.Log($"Error in OnUpdate: {ex.Message}");
            }
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (CurrentChart == null || !ShowBG)
                return;

            var graphics = args.Graphics;
            var mainWindow = CurrentChart.MainWindow;
            var lastDateTime = Symbol.LastDateTime;

            foreach (var trade in tradeHistory)
            {
                DateTime endTime = trade.EndTime ?? lastDateTime; // Use current time if trade is still open

                int leftCoordinate = (int)mainWindow.CoordinatesConverter.GetChartX(trade.StartTime);
                int rightCoordinate = (int)mainWindow.CoordinatesConverter.GetChartX(endTime);

                Color tradeColor = trade.IsLong ? BullColor : BearColor;
                graphics.FillRectangle(
                    new SolidBrush(Color.FromArgb(50, tradeColor)),
                    leftCoordinate,
                    0,
                    rightCoordinate - leftCoordinate,
                    mainWindow.ClientRectangle.Height
                );
            }
        }

        protected override void OnClear()
        {
            base.OnClear();
            tradeHistory.Clear();
            currentTrade = null;
        }

        protected override void OnSettingsUpdated()
        {
            base.OnSettingsUpdated();

            Refresh();
        }

    }
}