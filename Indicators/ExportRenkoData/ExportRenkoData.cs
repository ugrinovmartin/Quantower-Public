using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;
using TradingPlatform.BusinessLayer.History.Aggregations;

namespace ExportRenkoData
{
    public class ExportRenkoData : Indicator
    {
        [InputParameter("Brick Size", 0, 1, 100, 1, 0)]
        public int brickSize = 40;

        [InputParameter("From Time", 1)]
        public DateTime fromTime = DateTime.Now.AddDays(-30);

        [InputParameter("To Time", 2)]
        public DateTime toTime = DateTime.Now;

        [InputParameter("Extension", 3, 1, 1000, 1, 0)]
        public int extension = 100;

        [InputParameter("Reversal", 4, 1, 1000, 1, 0)]
        public int reversal = 100;


        private HistoricalData historicalData;
        private Rectangle buttonBounds;
        private bool buttonClicked = false;
        private readonly Timer flashTimer = new(500);
        private bool isLoadingData = true;


        protected static string LoadingMessage => loc._("Loading data...");

        public ExportRenkoData()
            : base()
        {
            Name = "Export Renko Data";
            Description = "Exports Renko candle data to a CSV file.";

            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            // Load historical data
            Task.Run(() =>
            {
                this.historicalData = this.Symbol.GetHistory(new HistoryRequestParameters()
                {
                    Symbol = this.Symbol,
                    FromTime = fromTime,
                    ToTime = toTime,
                    HistoryType = Symbol.HistoryType,
                    Aggregation = new HistoryAggregationRenko(Period.TICK1, this.brickSize, RenkoStyle.AdvancedClassic, this.extension, this.reversal, showWicks: true),
                    ForceReload = true
                });

                isLoadingData = false;
                CurrentChart.Refresh();
            });

            var parameters = new HistoryAggregationVwapParameters()
            {
                Aggregation = new HistoryAggregationRenko(Period.TICK1, this.brickSize, RenkoStyle.AdvancedClassic, this.extension, this.reversal, showWicks: true),
                DataType = VwapDataType.CurrentTF,
                Period = Period.DAY1,
                PriceType = VwapPriceType.HLC3,
                StdCalculationType = VwapStdCalculationType.StandardDeviation,
                TimeZone = Core.Instance.TimeUtils.SelectedTimeZone,
            };

            int assumedChartWidth = 800;
            int buttonWidth = 100;
            int padding = 10;
            int buttonX = assumedChartWidth - buttonWidth - padding;

            // Initialize buttonBounds with the new X position
            buttonBounds = new Rectangle(buttonX, 50, buttonWidth, 30);

            // Subscribe to the MouseClick event
            this.CurrentChart.MouseClick += CurrentChart_MouseClick;

            flashTimer.Elapsed += (sender, args) =>
            {
                buttonClicked = false;
                flashTimer.Stop();
                this.CurrentChart.Refresh();
            };
            flashTimer.AutoReset = false;

        }

        private void CurrentChart_MouseClick(object sender, ChartMouseNativeEventArgs e)
        {
            if (buttonBounds.Contains(e.Location))
            {
                ExportDataToCsv();
                buttonClicked = true;
                flashTimer.Start();
                CurrentChart.Refresh();
            }
        }
        private void ExportDataToCsv()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RenkoData100200.csv");

            using (var writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("Timestamp,BuildTime,Open,High,Low,Close,Volume,State");

                for (int i = 1; i < historicalData.Count; i++)
                {
                    var bar = (HistoryItemBar)historicalData[i];

                    int state = bar.Close > bar.Open ? 1 : 2;

                    TimeSpan buildTime = bar.TimeRight - bar.TimeLeft;
                    double buildTimeSeconds = Math.Round(buildTime.TotalSeconds, 2);

                    writer.WriteLine($"{bar.TimeLeft:HH:mm:ss},{buildTimeSeconds},{bar.Open},{bar.High},{bar.Low},{bar.Close},{bar.Volume},{state}");
                }
            }
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            var gr = args.Graphics;

            if (isLoadingData)
            {

                using var loadingFont = new Font("Verdana", 10, FontStyle.Regular, GraphicsUnit.Point);
                StringFormat centerCenterSF = new()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                gr.DrawString(LoadingMessage, loadingFont, Brushes.DodgerBlue, args.Rectangle, centerCenterSF);
            }
            else
            {
                // Determine button color based on click state
                Color buttonColor = buttonClicked ? Color.Red : Color.DarkGreen;

                // Draw the button with the determined color
                using (var brush = new SolidBrush(buttonColor))
                {
                    gr.FillRectangle(brush, buttonBounds);
                }

                // Draw the button border and text
                using (var pen = new Pen(Color.White))
                {
                    gr.DrawRectangle(pen, buttonBounds);
                }
                using (var buttonFont = new Font("Arial", 8))
                {
                    var buttonSF = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    gr.DrawString("Export CSV", buttonFont, Brushes.White, buttonBounds, buttonSF);
                }
            }
        }


        protected override void OnClear()
        {
            // Unsubscribe from the MouseClick event
            this.CurrentChart.MouseClick -= CurrentChart_MouseClick;
        }
    }
}
