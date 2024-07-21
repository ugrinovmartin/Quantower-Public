
using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace BarsDataIndicators.Utils;

public abstract class CandleDrawIndicator : Indicator
{

    private const int BAR_WIDTH_LIMIT_PX = 50;

    private Color upBarColor;
    protected Color UpBarColor
    {
        get => this.upBarColor;
        set
        {
            this.upBarColor = value;
            this.upBarPen = new Pen(value, this.barWickLineWidth);
        }
    }
    private Pen upBarPen;

    private Color downBarColor;
    protected Color DownBarColor
    {
        get => this.downBarColor;
        set
        {
            this.downBarColor = value;
            this.downBarPen = new Pen(value, this.barWickLineWidth);
        }
    }
    private Pen downBarPen;

    public HistoricalDataCustom candleSourceHD;

    private int BarWickLineWidth
    {
        get => this.barWickLineWidth;
        set
        {
            if (this.barWickLineWidth == value)
                return;

            this.barWickLineWidth = value;

            if (this.downBarPen != null)
                this.downBarPen.Width = value;

            if (this.upBarPen != null)
                this.upBarPen.Width = value;
        }
    }
    private int barWickLineWidth;

    protected static string LoadingMessage => loc._("Loading custom volume data...");
    protected bool IsLoading { get; set; }

    private readonly Font font;
    private readonly StringFormat centerCenterSF;

    public CandleDrawIndicator()
    {
        this.AddLineSeries("Open", Color.Transparent, 1, LineStyle.Points);
        this.AddLineSeries("High", Color.Transparent, 1, LineStyle.Points);
        this.AddLineSeries("Low", Color.Transparent, 1, LineStyle.Points);
        this.AddLineSeries("Close", Color.Transparent, 1, LineStyle.Points);

        this.IsUpdateTypesSupported = false;

        this.LinesSeries[0].ShowLineMarker = false;
        this.LinesSeries[1].ShowLineMarker = false;
        this.LinesSeries[2].ShowLineMarker = false;

        this.SeparateWindow = true;
        this.UpBarColor = Color.FromArgb(55, 219, 186);
        this.DownBarColor = Color.FromArgb(235, 96, 47);
        this.barWickLineWidth = 1;

        this.font = new Font("Verdana", 10, FontStyle.Regular, GraphicsUnit.Point);
        this.centerCenterSF = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        this.IsLoading = true;
    }

    protected override void OnInit()
    {
        candleSourceHD = new HistoricalDataCustom(this);

    }

    protected override void OnClear() => candleSourceHD.Dispose();

    public override IList<SettingItem> Settings
    {
        get
        {
            var settings = base.Settings;

            settings.Add(new SettingItemPairColor("BarStyle", new PairColor(UpBarColor, DownBarColor, loc._("Up"), loc._("Down")))
            {
                Text = loc._("Bar style"),
                SeparatorGroup = settings[0].SeparatorGroup
            });

            return settings;
        }
        set
        {
            if (value.GetItemByName("BarStyle")?.Value is PairColor pairColor)
            {
                this.UpBarColor = pairColor.Color1;
                this.DownBarColor = pairColor.Color2;
            }

            base.Settings = value;
        }
    }

    public override void OnPaintChart(PaintChartEventArgs args)
    {
        var gr = args.Graphics;
        var restoreClip = gr.ClipBounds;
        gr.SetClip(args.Rectangle);

        try
        {
            if (this.IsLoading)
            {
                gr.DrawString(LoadingMessage, this.font, Brushes.DodgerBlue, args.Rectangle, this.centerCenterSF);
            }
            else
            {
                if (this.HistoricalData.Count == 0)
                    return;

                var leftTime = this.CurrentChart.Windows[args.WindowIndex].CoordinatesConverter.GetTime(args.Rectangle.Left);
                var rightTime = this.CurrentChart.Windows[args.WindowIndex].CoordinatesConverter.GetTime(args.Rectangle.Right);

                int leftBarOffset = (int)this.HistoricalData.GetIndexByTime(leftTime.Ticks);
                int rightBarOffset = (int)this.HistoricalData.GetIndexByTime(rightTime.Ticks);

                if (rightBarOffset == -1)
                    rightBarOffset = 0;

                if (leftBarOffset == -1)
                    leftBarOffset = this.HistoricalData.Count - 1;

                float leftBarX = default;
                var currentWindow = this.CurrentChart.Windows[args.WindowIndex];
                float borderWidth = Math.Max(1f, this.CurrentChart.BarsWidth / 10); // 10% of bar width
                float visualBarWidth = Math.Max(1f, this.CurrentChart.BarsWidth - borderWidth * 2);

                this.BarWickLineWidth = this.CurrentChart.BarsWidth > BAR_WIDTH_LIMIT_PX ? 3 : 1;

                for (int i = leftBarOffset; i >= rightBarOffset; i--)
                {
                    if (this.candleSourceHD.Count <= i)
                        continue;

                    var time = this.HistoricalData[i].TimeLeft;
                    if (leftBarX == default)
                        leftBarX = (float)currentWindow.CoordinatesConverter.GetChartX(time) + borderWidth;
                    else
                        leftBarX += this.CurrentChart.BarsWidth;

                    double open = this.candleSourceHD[i][PriceType.Open];
                    double close = this.candleSourceHD[i][PriceType.Close];

                    if (double.IsNaN(open) || double.IsNaN(close))
                        continue;

                    bool isGrownBar = open < close;

                    float openY = (float)currentWindow.CoordinatesConverter.GetChartY(open);
                    float closeY = (float)currentWindow.CoordinatesConverter.GetChartY(close);
                    float height = isGrownBar ? openY - closeY : closeY - openY;
                    if (height < 1)
                        height = 1;

                    if (isGrownBar)
                        gr.FillRectangle(this.upBarPen.Brush, leftBarX, closeY, visualBarWidth, height);
                    else
                        gr.FillRectangle(this.downBarPen.Brush, leftBarX, openY, visualBarWidth, height);

                    if (visualBarWidth == 1)
                        continue;

                    // Draw bar wick
                    double high = this.candleSourceHD[i][PriceType.High];
                    double low = this.candleSourceHD[i][PriceType.Low];

                    if (double.IsNaN(high) || double.IsNaN(low))
                        continue;

                    if (high == open && low == close || high == close && low == open)
                        continue;

                    float highY = (float)this.CurrentChart.Windows[args.WindowIndex].CoordinatesConverter.GetChartY(high);
                    float lowY = (float)this.CurrentChart.Windows[args.WindowIndex].CoordinatesConverter.GetChartY(low);

                    float middleBarX = leftBarX + visualBarWidth / 2;
                    gr.DrawLine(isGrownBar ? this.upBarPen : this.downBarPen, middleBarX, highY, middleBarX, lowY);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Loggers.Log(ex);
        }
        finally
        {
            gr.SetClip(restoreClip);
        }
    }

    protected void SetValues(double open, double high, double low, double close, int offset)
    {
        if (!IsValidPrice(open) || !IsValidPrice(close))
            return;

        this.candleSourceHD.SetValue(open, high, low, close, offset);

        this.SetValue(open, 0, offset);
        this.SetValue(high, 1, offset);
        this.SetValue(low, 2, offset);
        this.SetValue(close, 3, offset);

    }

    protected void SetHole(int offset)
    {
        this.candleSourceHD.SetValue(double.NaN, double.NaN, double.NaN, double.NaN, offset);

        this.SetValue(double.NaN, 0, offset);
        this.SetValue(double.NaN, 1, offset);
        this.SetValue(double.NaN, 2, offset);
        this.SetValue(double.NaN, 3, offset);
    }

    protected static bool IsValidPrice(double price) => !double.IsNaN(price) && price != double.MinValue && price != double.MaxValue;

}