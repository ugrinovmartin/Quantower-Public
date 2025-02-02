using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;
using TradingPlatform.BusinessLayer.History.Aggregations;

namespace ExportRenkoData
{
    public class ExportRenkoData : Indicator
    {
        [InputParameter("Brick Size", 0, 1, 100, 1, 0)]
        public int brickSize = 40;

        [InputParameter("Extension", 3, 1, 1000, 1, 0)]
        public int extension = 100;

        [InputParameter("Reversal", 4, 1, 1000, 1, 0)]
        public int reversal = 100;

        [InputParameter("From Time", 1)]
        public DateTime fromTime = DateTime.Now.AddDays(-30);

        [InputParameter("To Time", 2)]
        public DateTime toTime = DateTime.Now;

        [InputParameter("CSV Filename", 5)]
        public string CsvFilename { get; set; } = "RenkoData.csv";

        private Rectangle buttonBounds;
        private bool buttonClicked = false;
        private bool isProcessing = false;
        private string statusMessage = "";

        public ExportRenkoData()
            : base()
        {
            Name = "Export Renko Data";
            Description = "Exports Renko candle data to a CSV file.";
        }

        protected override void OnInit()
        {
            int assumedChartWidth = 800;
            int buttonWidth = 100;
            int padding = 10;
            int buttonX = assumedChartWidth - buttonWidth - padding;
            buttonBounds = new Rectangle(buttonX, 50, buttonWidth, 30);

            this.CurrentChart.MouseClick += CurrentChart_MouseClick;
        }

        private void CurrentChart_MouseClick(object sender, ChartMouseNativeEventArgs e)
        {
            if (buttonBounds.Contains(e.Location) && !isProcessing)
            {
                buttonClicked = true;
                ExportDataWeekByWeek();
                this.CurrentChart.Refresh();
            }
        }

        private async void ExportDataWeekByWeek()
        {
            if (isProcessing) return;
            isProcessing = true;
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), CsvFilename);

            try
            {
                using (var writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine("Timestamp,BuildTime,Open,High,Low,Close,Volume,State");
                }

                await Task.Run(async () =>
                {
                    DateTime currentStart = fromTime;
                    while (currentStart < toTime)
                    {
                        try
                        {
                            DateTime weekEnd = currentStart.AddDays(7) > toTime ? toTime : currentStart.AddDays(7);
                            statusMessage = $"Processing week: {currentStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd}";
                            this.CurrentChart.Refresh();

                            Core.Loggers.Log($"Downloading week {currentStart:yyyy-MM-dd}");
                            var historicalData = this.Symbol.GetHistory(new HistoryRequestParameters()
                            {
                                Symbol = this.Symbol,
                                FromTime = currentStart,
                                ToTime = weekEnd,
                                HistoryType = Symbol.HistoryType,
                                Aggregation = new HistoryAggregationRenko(Period.TICK1, this.brickSize, RenkoStyle.AdvancedClassic, this.extension, this.reversal, showWicks: true),
                                ForceReload = true
                            });

                            Core.Loggers.Log($"Downloaded {historicalData.Count} history items");

                            if (historicalData.Count > 0)
                            {
                                using (var writer = new StreamWriter(filePath, true))
                                {
                                    for (int i = 1; i < historicalData.Count; i++)
                                    {
                                        var bar = (HistoryItemBar)historicalData[i];
                                        int state = bar.Close > bar.Open ? 1 : 2;
                                        TimeSpan buildTime = bar.TimeRight - bar.TimeLeft;
                                        double buildTimeSeconds = Math.Round(buildTime.TotalSeconds, 2);

                                        writer.WriteLine($"{bar.TimeLeft:HH:mm:ss},{buildTimeSeconds},{bar.Open},{bar.High},{bar.Low},{bar.Close},{bar.Volume},{state}");
                                    }
                                    writer.Flush();
                                }
                                Core.Loggers.Log($"Wrote data for week {currentStart:yyyy-MM-dd}");
                            }

                            historicalData.Dispose();
                            currentStart = weekEnd;
                        }
                        catch (Exception ex)
                        {
                            Core.Loggers.Log($"Error processing week {currentStart:yyyy-MM-dd}: {ex.Message}");
                            statusMessage = $"Error on week {currentStart:yyyy-MM-dd}. Previous weeks saved. Error: {ex.Message}";
                            currentStart = currentStart.AddDays(7);
                        }
                    }
                });

                statusMessage = "Export completed successfully!";
            }
            catch (Exception ex)
            {
                Core.Loggers.Log($"Fatal error during export: {ex.Message}");
                statusMessage = $"Export failed: {ex.Message}";
            }
            finally
            {
                isProcessing = false;
                this.CurrentChart.Refresh();
            }
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            var gr = args.Graphics;

            Color buttonColor = isProcessing ? Color.Gray : (buttonClicked ? Color.DarkGreen : Color.Blue);
            using (var brush = new SolidBrush(buttonColor))
            {
                gr.FillRectangle(brush, buttonBounds);
            }

            using (var pen = new Pen(Color.White))
            {
                gr.DrawRectangle(pen, buttonBounds);
            }
            using (var buttonFont = new Font("Arial", 8))
            {
                var buttonSF = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                gr.DrawString("Export CSV", buttonFont, Brushes.White, buttonBounds, buttonSF);
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                using (var statusFont = new Font("Arial", 10))
                {
                    var statusBounds = new Rectangle(10, 50, 400, 30);
                    gr.DrawString(statusMessage, statusFont, Brushes.White, statusBounds);
                }
            }
        }

        protected override void OnClear()
        {
            this.CurrentChart.MouseClick -= CurrentChart_MouseClick;
        }
    }
}