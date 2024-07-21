// Copyright QUANTOWER LLC. � 2017-2024. All rights reserved.

using BarsDataIndicators.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace BarsDataIndicators;

public class IndicatorCumulativeDelta : CandleDrawIndicator, IVolumeAnalysisIndicator, ISessionObserverIndicator
{

    // 

    // Added EMA line calculation based on the cumulative delta candles
    // Bug: Adjusting EMA Period from Setting needed to recalculate with proper values 

     //

    #region Consts

    private const string BY_VOLUME_TYPE = "By volume";
    private const string BY_TRADES_TYPE = "By trades";

    private const string CHART_SESSION_CONTAINER_SELECT_ITEM = "Chart session";

    private const string CUSTOM_OPEN_SESSION_NAME_SI = "Open time";
    private const string CUSTOM_CLOSE_SESSION_NAME_SI = "Close time";
    private const string RESET_PERIOD_NAME_SI = "ResetPeriod";
    private const string SESSION_TEMPLATE_NAME_SI = "sessionsTemplate";

    private const string RESET_TYPE_NAME_SI = "Reset type";
    private const string BY_PERIOD_SESSION_TYPE = "By period";
    private const string FULL_HISTORY_SESSION_TYPE = "Full range";
    private const string SPECIFIED_SESSION_TYPE = "Specified session";
    private const string CUSTOM_RANGE_SESSION_TYPE = "Custom range";

    #endregion Consts

    #region Parameters

    [InputParameter("EMA period", 8, 1, int.MaxValue, 1, 0)]
    public int EmaPeriod = 14;

    private double k;
    private List<double> emaValues;
    private bool isHistoricalDataLoaded;

    [InputParameter("Delta source", 9, variants: new object[]
    {
        BY_VOLUME_TYPE, CumulativeDeltaSourceType.Volume,
        BY_TRADES_TYPE, CumulativeDeltaSourceType.Trades
    })]
    public CumulativeDeltaSourceType DeltaSourceType;

    [InputParameter(RESET_TYPE_NAME_SI, 30, variants: new object[]
    {
        BY_PERIOD_SESSION_TYPE, CumulativeDeltaSessionMode.ByPeriod,
        FULL_HISTORY_SESSION_TYPE, CumulativeDeltaSessionMode.FullHistory,
        SPECIFIED_SESSION_TYPE, CumulativeDeltaSessionMode.SpecifiedSession,
        CUSTOM_RANGE_SESSION_TYPE, CumulativeDeltaSessionMode.CustomRange
    })]
    public CumulativeDeltaSessionMode SessionMode;

    public ISessionsContainer SessionContainer
    {
        get
        {
            switch (this.SessionMode)
            {
                case CumulativeDeltaSessionMode.SpecifiedSession:
                    {
                        if (this.specifiedSessionContainerId == CHART_SESSION_CONTAINER_SELECT_ITEM)
                            return this.CurrentChart?.CurrentSessionContainer;
                        else
                            return this.selectedSessionContainer;
                    }
                case CumulativeDeltaSessionMode.CustomRange:
                    return this.customSessionContainer;
                default:
                case CumulativeDeltaSessionMode.ByPeriod:
                case CumulativeDeltaSessionMode.FullHistory:
                    return this.fullDaySessionContainer;
            }
        }
    }
    private string specifiedSessionContainerId;
    private ISessionsContainer customSessionContainer;
    private ISessionsContainer fullDaySessionContainer;
    private ISessionsContainer selectedSessionContainer;

    public DateTime CustomRangeStartTime
    {
        get
        {
            if (this.customRangeStartTime == default)
            {
                var session = this.GetFullDayTimeInterval(this.GetTimeZone());
                this.customRangeStartTime = session.From;
            }
            return this.customRangeStartTime;
        }
        set => this.customRangeStartTime = value;
    }
    private DateTime customRangeStartTime;

    public DateTime CustomRangeEndTime
    {
        get
        {
            if (this.customRangeEndTime == default)
            {
                var session = this.GetFullDayTimeInterval(this.GetTimeZone());
                this.customRangeEndTime = session.To;
            }

            return this.customRangeEndTime;
        }
        set => this.customRangeEndTime = value;
    }
    private DateTime customRangeEndTime;

    public Period ResetPeriod { get; private set; }

    public override string ShortName
    {
        get
        {
            return this.DeltaSourceType switch
            {
                CumulativeDeltaSourceType.Volume => $"{this.Name} ({BY_VOLUME_TYPE})",
                CumulativeDeltaSourceType.Trades => $"{this.Name} ({BY_TRADES_TYPE})",

                _ => this.Name,
            };
        }
    }

    private AreaBuilder currentAreaBuider;

    #endregion Parameters

    public IndicatorCumulativeDelta()
        : base()
    {
        this.Name = "Cumulative delta";

        this.LinesSeries[0].Name = "Cumulative open";
        this.LinesSeries[1].Name = "Cumulative high";
        this.LinesSeries[2].Name = "Cumulative low";
        this.LinesSeries[3].Name = "Cumulative close";

        this.AddLineSeries("EMA", Color.Orange, 1, LineStyle.Solid);

        this.AddLineLevel(0d, "Zero line", Color.Gray, 1, LineStyle.DashDot);

        this.DeltaSourceType = CumulativeDeltaSourceType.Volume;
        this.SessionMode = CumulativeDeltaSessionMode.ByPeriod;
        this.ResetPeriod = Period.DAY1;

        this.SeparateWindow = true;

        this.emaValues = new List<double>();
        this.isHistoricalDataLoaded = false;
    }

    #region Overrides

    protected override void OnInit()
    {
        switch (this.SessionMode)
        {
            case CumulativeDeltaSessionMode.CustomRange:
                {
                    this.customSessionContainer = new CustomSessionsContainer("CustomSession", this.GetTimeZone(), new CustomSession[]
                    {
                        this.CreateCustomSession(this.CustomRangeStartTime.TimeOfDay,this.CustomRangeEndTime.TimeOfDay, this.GetTimeZone().TimeZoneInfo)
                    });
                    break;
                }
            case CumulativeDeltaSessionMode.FullHistory:
            case CumulativeDeltaSessionMode.ByPeriod:
                {
                    var timeZone = this.GetTimeZone();
                    var session = this.GetFullDayTimeInterval(timeZone);
                    this.fullDaySessionContainer = new CustomSessionsContainer("FullDaySession", timeZone, new CustomSession[]
                    {
                        this.CreateCustomSession(session.From.TimeOfDay, session.To.TimeOfDay, this.GetTimeZone().TimeZoneInfo)
                    });
                    break;
                }
        }

        this.EmaPeriod = 14;

        base.OnInit();
    }

    protected override void OnUpdate(UpdateArgs args)
    {
        if (this.IsLoading)
            return;

        var currentVolumeData = this.GetVolumeAnalysisData(0);

        // Recalculate previous items if needed
        if (currentVolumeData?.Total == null)
        {
            var currentIndex = Math.Max(0, this.Count - 1);

            if (this.currentAreaBuider.BarIndex != currentIndex)
            {
                for (int i = this.currentAreaBuider.BarIndex; i < this.Count; i++)
                {
                    var offset = Math.Max(0, this.Count - i - 1);

                    var prevVolumeDataItem = this.GetVolumeAnalysisData(offset);
                    if (prevVolumeDataItem == null)
                        continue;

                    this.CalculateIndicatorByOffset(offset, true, true);
                }
            }
        }
        else
        {
            var isNewBar = args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.HistoricalBar;
            this.CalculateIndicatorByOffset(0, isNewBar, false);
        }

        // Calculate and update the EMA using custom historical data
        double closePrice = this.GetClosePrice(0);

        if (this.emaValues.Count == 0)
        {
            // Initialize EMA from history
            if (!this.isHistoricalDataLoaded)
            {
                this.InitializeEmaFromHistory();
                this.isHistoricalDataLoaded = true;
            }
            // If there are no EMA values yet, initialize with the first close price
            else
            {
                this.emaValues.Add(closePrice);
                this.SetValue(closePrice, 4);
            }
        }

        if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.HistoricalBar)
        {

            // Initialize EMA calculation variables
            this.k = 2.0 / (this.EmaPeriod + 1);

            // EMA formula
            double prevEmaValue = this.emaValues.Last();
            double emaValue = (closePrice - prevEmaValue) * this.k + prevEmaValue;
            this.emaValues.Add(emaValue);

            // Set the EMA value for the line series
            this.SetValue(emaValue, 4);
        }
    }

    private void InitializeEmaFromHistory()
    {
        if (this.Count >= this.EmaPeriod)
        {

            // Initialize EMA calculation variables
            this.k = 2.0 / (this.EmaPeriod + 1);

            // Calculate the initial EMA using SMA
            double sum = 0;
            for (int i = this.EmaPeriod - 1; i >= 0; i--)
            {
                sum += this.GetClosePrice(i);
            }
            double initialEma = sum / this.EmaPeriod;
            this.emaValues.Add(initialEma);

            // Update EMA for remaining historical data
            for (int i = this.EmaPeriod; i < this.Count; i++)
            {
                double closePrice = this.GetClosePrice(i);
                double prevEmaValue = this.emaValues.Last();
                double emaValue = (closePrice - prevEmaValue) * this.k + prevEmaValue;
                this.emaValues.Add(emaValue);
                this.SetValue(emaValue, 4, this.Count - i - 1);
            }
        }
    }

    protected override void OnClear()
    {
        if (this.currentAreaBuider != null)
        {
            this.currentAreaBuider.Dispose();
            this.currentAreaBuider = null;
        }

        this.emaValues.Clear();
        this.isHistoricalDataLoaded = false;

        base.OnClear();
    }
    protected override void OnSettingsUpdated() => base.OnSettingsUpdated();

    public override IList<SettingItem> Settings
    {
        get
        {
            var settings = base.Settings;

            var separ = settings.FirstOrDefault()?.SeparatorGroup;

            var defaultItem = new SelectItem(CHART_SESSION_CONTAINER_SELECT_ITEM);
            var items = new List<SelectItem> { defaultItem };
            items.AddRange(Core.Instance.CustomSessions.Select(s => new SelectItem(s.Name, s.Id)));

            var selectedItem = items.FirstOrDefault(i => i.Value.Equals(this.specifiedSessionContainerId)) ?? items.First();

            settings.Add(new SettingItemSelectorLocalized(SESSION_TEMPLATE_NAME_SI, selectedItem, items, 20)
            {
                Text = loc._("Sessions template"),
                SeparatorGroup = separ,
                Relation = new SettingItemRelationVisibility(RESET_TYPE_NAME_SI, new SelectItem("", (int)CumulativeDeltaSessionMode.SpecifiedSession))
            });

            settings.Add(new SettingItemDateTime(CUSTOM_OPEN_SESSION_NAME_SI, this.CustomRangeStartTime, 30)
            {
                ValueChangingBehavior = SettingItemValueChangingBehavior.WithConfirmation,
                Format = DatePickerFormat.Time,
                SeparatorGroup = separ,
                Relation = new SettingItemRelationVisibility(RESET_TYPE_NAME_SI, new SelectItem("", (int)CumulativeDeltaSessionMode.CustomRange))
            });
            settings.Add(new SettingItemDateTime(CUSTOM_CLOSE_SESSION_NAME_SI, this.CustomRangeEndTime, 30)
            {
                ValueChangingBehavior = SettingItemValueChangingBehavior.WithConfirmation,
                Format = DatePickerFormat.Time,
                SeparatorGroup = separ,
                Relation = new SettingItemRelationVisibility(RESET_TYPE_NAME_SI, new SelectItem("", (int)CumulativeDeltaSessionMode.CustomRange))
            });

            settings.Add(new SettingItemPeriod(RESET_PERIOD_NAME_SI, this.ResetPeriod, 30)
            {
                Text = loc._("Period"),
                ExcludedPeriods = new BasePeriod[] { BasePeriod.Tick, BasePeriod.Second, BasePeriod.Minute, BasePeriod.Hour, BasePeriod.Year },
                SeparatorGroup = separ,
                Relation = new SettingItemRelationVisibility(RESET_TYPE_NAME_SI, new SelectItem("", (int)CumulativeDeltaSessionMode.ByPeriod))
            });

            return settings;
        }
        set
        {
            var holder = new SettingsHolder(value);
            base.Settings = value;

            var needRefresh = false;

            if (holder.TryGetValue(SESSION_TEMPLATE_NAME_SI, out var item))
            {
                var newContainerId = item.GetValue<string>();

                if (newContainerId != this.specifiedSessionContainerId)
                {
                    this.specifiedSessionContainerId = newContainerId;
                    this.selectedSessionContainer = Core.Instance.CustomSessions[this.specifiedSessionContainerId];
                    needRefresh |= item.ValueChangingReason == SettingItemValueChangingReason.Manually;
                }
            }

            if (holder.TryGetValue(RESET_PERIOD_NAME_SI, out item))
            {
                var newValue = item.GetValue<Period>();

                if (this.ResetPeriod != newValue)
                {
                    this.ResetPeriod = newValue;
                    needRefresh |= item.ValueChangingReason == SettingItemValueChangingReason.Manually;
                }
            }

            if (holder.TryGetValue(CUSTOM_OPEN_SESSION_NAME_SI, out item))
            {
                var newValue = item.GetValue<DateTime>();

                if (this.CustomRangeStartTime != newValue)
                {
                    this.CustomRangeStartTime = newValue;
                    needRefresh |= item.ValueChangingReason == SettingItemValueChangingReason.Manually;
                }
            }

            if (holder.TryGetValue(CUSTOM_CLOSE_SESSION_NAME_SI, out item))
            {
                var newValue = item.GetValue<DateTime>();

                if (this.CustomRangeEndTime != newValue)
                {
                    this.CustomRangeEndTime = newValue;
                    needRefresh |= item.ValueChangingReason == SettingItemValueChangingReason.Manually;
                }
            }

            if (needRefresh)
                this.Refresh();
        }
    }

    #endregion Overrides

    #region Misc

    private void CalculateIndicatorByOffset(int offset, bool isNewBar, bool createAfterUpdate = false)
    {
        if (this.Count <= offset)
            return;

        var time = this.Time(offset);

        var index = Math.Max(this.Count - offset - 1, 0);

        if (this.SessionMode == CumulativeDeltaSessionMode.SpecifiedSession || this.SessionMode == CumulativeDeltaSessionMode.CustomRange)
        {
            if (!this.SessionContainer.ContainsDate(time.Ticks))
            {
                this.currentAreaBuider?.Reset(index);
                return;
            }
        }

        if (this.currentAreaBuider == null)
        {
            var period = this.GetStepPeriod();
            var range = this.SessionMode != CumulativeDeltaSessionMode.FullHistory
                ? HistoryStepsCalculator.GetSteps(time, time.AddTicks(period.Ticks), period.BasePeriod, period.PeriodMultiplier, this.SessionContainer, this.GetTimeZone()).FirstOrDefault()
                : new Interval<DateTime>(time, DateTime.MaxValue);

            if (range.IsEmpty)
                return;

            this.currentAreaBuider = this.CreateAreaBuilder(range);
        }
        else if (!this.currentAreaBuider.Contains(time))
        {
            var period = this.GetStepPeriod();
            var range = HistoryStepsCalculator.GetNextStep(this.currentAreaBuider.Range.To, period.BasePeriod, period.PeriodMultiplier);

            if (range.IsEmpty)
                return;

            this.currentAreaBuider = this.CreateAreaBuilder(range);
        }

        var currentItem = this.GetVolumeAnalysisData(offset);
        if (currentItem == null || currentItem.Total == null)
            return;

        if (isNewBar && !createAfterUpdate)
            this.currentAreaBuider.StartNew(index);

        this.currentAreaBuider.Update(currentItem.Total);

        this.SetValues(this.currentAreaBuider.Bar.Open, this.currentAreaBuider.Bar.High, this.currentAreaBuider.Bar.Low, this.currentAreaBuider.Bar.Close, offset);

        if (isNewBar && createAfterUpdate)
            this.currentAreaBuider.StartNew(++index);
    }

    private double GetClosePrice(int index)
    {
        if (index < 0 || index >= this.candleSourceHD.Count)
            throw new IndexOutOfRangeException("Index is out of range.");

        return this.candleSourceHD[index][PriceType.Close];
    }

    private Interval<DateTime> GetFullDayTimeInterval(TradingPlatform.BusinessLayer.TimeZone timeZone)
    {
        var openTime = new DateTime(DateTime.UtcNow.Date.Ticks, DateTimeKind.Unspecified);
        var closeTime = new DateTime(DateTime.UtcNow.Date.AddDays(-1).Ticks, DateTimeKind.Unspecified);

        return new Interval<DateTime>(Core.Instance.TimeUtils.ConvertFromTimeZoneToUTC(openTime, timeZone), Core.Instance.TimeUtils.ConvertFromTimeZoneToUTC(closeTime, timeZone));
    }

    private TradingPlatform.BusinessLayer.TimeZone GetTimeZone()
    {
        return this.CurrentChart?.CurrentTimeZone ?? Core.Instance.TimeUtils.SelectedTimeZone;
    }

    private Period GetStepPeriod()
    {
        if (this.SessionMode == CumulativeDeltaSessionMode.ByPeriod)
            return this.ResetPeriod;
        else
            return Period.DAY1;
    }

    private CustomSession CreateCustomSession(TimeSpan open, TimeSpan close, TimeZoneInfo info)
    {
        var session = new CustomSession
        {
            OpenOffset = open,
            CloseOffset = close,
            IsActive = true,
            Name = "Main",
            Days = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToArray(),
            Type = SessionType.Main
        };
        session.RecalculateOpenCloseTime(info);
        return session;
    }

    private AreaBuilder CreateAreaBuilder(Interval<DateTime> range)
    {
        return this.DeltaSourceType switch
        {
            CumulativeDeltaSourceType.Trades => new AreaBuilderByTrades(range),
            _ => new AreaBuilderByVolume(range),
        };
    }

    #endregion Misc

    #region IVolumeAnalysisIndicator

    bool IVolumeAnalysisIndicator.IsRequirePriceLevelsCalculation => false;

    public void VolumeAnalysisData_Loaded()
    {
        if (this.currentAreaBuider != null)
        {
            this.currentAreaBuider.Dispose();
            this.currentAreaBuider = null;
        }

        for (int i = this.Count - 1; i >= 0; i--)
            this.CalculateIndicatorByOffset(i, true);

        this.IsLoading = false;
    }

    #endregion IVolumeAnalysisIndicator

    #region Nested

    public enum CumulativeDeltaSessionMode
    {
        ByPeriod,
        FullHistory,
        SpecifiedSession,
        CustomRange
    }

    public enum CumulativeDeltaSourceType
    {
        Volume,
        Trades
    }

    abstract class AreaBuilder : IDisposable
    {
        internal Interval<DateTime> Range { get; }
        internal BarBuilder Bar { get; private set; }
        internal int BarIndex { get; private set; }

        public AreaBuilder(Interval<DateTime> range)
        {
            this.Range = range;

            this.Bar = new BarBuilder();
            this.StartNew(0);
        }

        internal abstract void Update(VolumeAnalysisItem total);

        internal void StartNew(int barIndex)
        {
            var prevClose = !double.IsNaN(this.Bar.Close) ? this.Bar.Close : 0d;

            this.Bar.Clear();
            this.Bar.Open = prevClose;
            this.BarIndex = barIndex;
        }

        internal bool Contains(DateTime dt)
        {
            if (dt.CompareTo(this.Range.Min) < 0)
                return false;

            if (dt.CompareTo(this.Range.Max) >= 0)
                return false;

            return true;
        }

        internal void Reset(int barIndex)
        {
            this.Bar.Clear();
            this.Bar.Open = 0;
            this.BarIndex = barIndex;
        }

        public void Dispose()
        {
            this.Bar = null;
        }
    }
    sealed class AreaBuilderByVolume : AreaBuilder
    {
        public AreaBuilderByVolume(Interval<DateTime> range)
            : base(range)
        { }

        internal override void Update(VolumeAnalysisItem total)
        {
            this.Bar.Close = this.Bar.Open + total.Delta;

            this.Bar.High = !double.IsNaN(total.MaxDelta) && total.MaxDelta != double.MinValue
                ? this.Bar.Open + Math.Abs(total.MaxDelta)
                : Math.Max(this.Bar.Close, this.Bar.Open);

            this.Bar.Low = !double.IsNaN(total.MinDelta) && total.MinDelta != double.MaxValue
                ? this.Bar.Open - Math.Abs(total.MinDelta)
                : Math.Min(this.Bar.Close, this.Bar.Open);
        }
    }
    sealed class AreaBuilderByTrades : AreaBuilder
    {
        public AreaBuilderByTrades(Interval<DateTime> range)
            : base(range)
        { }

        internal override void Update(VolumeAnalysisItem total)
        {
            this.Bar.Close = this.Bar.Open + (total.BuyTrades - total.SellTrades);
            this.Bar.High = Math.Max(this.Bar.Close, this.Bar.Open);
            this.Bar.Low = Math.Min(this.Bar.Close, this.Bar.Open);
        }
    }

    internal class BarBuilder
    {
        public double Open { get; internal set; }
        public double High { get; internal set; }
        public double Low { get; internal set; }
        public double Close { get; internal set; }

        public BarBuilder()
        {
            this.Clear();
        }

        internal void Clear()
        {
            this.Open = double.NaN;
            this.High = double.NaN;
            this.Low = double.NaN;
            this.Close = double.NaN;
        }
    }

    #endregion Nested
}
