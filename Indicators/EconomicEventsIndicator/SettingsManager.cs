using System;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace EconomicEventsIndicator
{
    public class SettingsManager
    {
        private EconomicEventsIndicator indicator;

        public SettingsManager(EconomicEventsIndicator indicator)
        {
            this.indicator = indicator;
        }

        public IList<SettingItem> GetSettings()
        {
            var settings = indicator.Settings;

            var chartDateSelector = new SelectItem("Current Chart Date", 1);
            var customDateSelector = new SelectItem("Custom Date", 2);

            settings.Add(new SettingItemSelectorLocalized("dateMode", new SelectItem("dateMode", indicator.dateMode), new List<SelectItem>
                     {
                        chartDateSelector,
                        customDateSelector
                     })
            {
                Text = "Select Date:"
            });

            settings.Add(new SettingItemDateTime("customStartDate", indicator.customStartDate)
            {
                Text = "From Date",
                Relation = new SettingItemRelationVisibility("dateMode", customDateSelector)
            });

            settings.Add(new SettingItemDateTime("customEndDate", indicator.customEndDate)
            {
                Text = "To Date",
                Relation = new SettingItemRelationVisibility("dateMode", customDateSelector)
            });

            settings.Add(new SettingItemBoolean("highImpact", indicator.highImpact)
            {
                Text = "High Impact"
            });
            settings.Add(new SettingItemBoolean("mediumImpact", indicator.mediumImpact)
            {
                Text = "Medium Impact"
            });
            settings.Add(new SettingItemBoolean("lowImpact", indicator.lowImpact)
            {
                Text = "Low Impact"
            });
            settings.Add(new SettingItemBoolean("nonEconomicNews", indicator.nonEconomicNews)
            {
                Text = "Non-Economic Events"
            });

            var allCurrenciesSelector = new SelectItem("All", 1);
            var selectCurrencySelector = new SelectItem("Select Currency", 2);

            settings.Add(new SettingItemSelectorLocalized("currencyMode", new SelectItem("currencyMode", indicator.currencyMode), new List<SelectItem>
                     {
                        allCurrenciesSelector,
                        selectCurrencySelector
                     })
            {
                Text = "Currency:"
            });

            settings.Add(new SettingItemBoolean("usdSelected", indicator.usdSelected)
            {
                Text = "USD",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });
            settings.Add(new SettingItemBoolean("eurSelected", indicator.eurSelected)
            {
                Text = "EUR",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });
            settings.Add(new SettingItemBoolean("audSelected", indicator.audSelected)
            {
                Text = "AUD",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });
            settings.Add(new SettingItemBoolean("cadSelected", indicator.cadSelected)
            {
                Text = "CAD",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });
            settings.Add(new SettingItemBoolean("chfSelected", indicator.chfSelected)
            {
                Text = "CHF",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });
            settings.Add(new SettingItemBoolean("cnySelected", indicator.cnySelected)
            {
                Text = "CNY",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });
            settings.Add(new SettingItemBoolean("gbpSelected", indicator.gbpSelected)
            {
                Text = "GBP",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });
            settings.Add(new SettingItemBoolean("jpySelected", indicator.jpySelected)
            {
                Text = "JPY",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });
            settings.Add(new SettingItemBoolean("nzdSelected", indicator.nzdSelected)
            {
                Text = "NZD",
                Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
            });

            settings.Add(new SettingItemSelectorLocalized("timeZoneMode", new SelectItem("timeZoneMode", indicator.timeZoneMode), new List<SelectItem>
                     {
                        new SelectItem("Current Chart Timezone", 1),
                        new SelectItem("System Timezone", 2)
                     })
            {
                Text = "Time Zone:"
            });

            settings.Add(new SettingItemInteger("newsPositionX", indicator.newsPositionX)
            {
                Text = "Move Left/Right (-/+)"
            });

            settings.Add(new SettingItemInteger("newsPositionY", indicator.newsPositionY)
            {
                Text = "Move Up/Down (-/+)"
            });

            return settings;
        }

        public void UpdateSettings(IList<SettingItem> settings)
        {
            if (settings.TryGetValue("dateMode", out int dateModeValue))
                indicator.dateMode = dateModeValue;

            if (settings.TryGetValue("customStartDate", out DateTime customStartDateValue))
                indicator.customStartDate = customStartDateValue;

            if (settings.TryGetValue("customEndDate", out DateTime customEndDateValue))
                indicator.customEndDate = customEndDateValue;

            if (settings.TryGetValue("highImpact", out bool highImpactValue))
                indicator.highImpact = highImpactValue;
            if (settings.TryGetValue("mediumImpact", out bool mediumImpactValue))
                indicator.mediumImpact = mediumImpactValue;
            if (settings.TryGetValue("lowImpact", out bool lowImpactValue))
                indicator.lowImpact = lowImpactValue;
            if (settings.TryGetValue("nonEconomicNews", out bool nonEconomicNewsValue))
                indicator.nonEconomicNews = nonEconomicNewsValue;

            if (settings.TryGetValue("currencyMode", out int currencyModeValue))
                indicator.currencyMode = currencyModeValue;

            if (settings.TryGetValue("audSelected", out bool audSelectedValue))
                indicator.audSelected = audSelectedValue;
            if (settings.TryGetValue("cadSelected", out bool cadSelectedValue))
                indicator.cadSelected = cadSelectedValue;
            if (settings.TryGetValue("chfSelected", out bool chfSelectedValue))
                indicator.chfSelected = chfSelectedValue;
            if (settings.TryGetValue("cnySelected", out bool cnySelectedValue))
                indicator.cnySelected = cnySelectedValue;
            if (settings.TryGetValue("eurSelected", out bool eurSelectedValue))
                indicator.eurSelected = eurSelectedValue;
            if (settings.TryGetValue("gbpSelected", out bool gbpSelectedValue))
                indicator.gbpSelected = gbpSelectedValue;
            if (settings.TryGetValue("jpySelected", out bool jpySelectedValue))
                indicator.jpySelected = jpySelectedValue;
            if (settings.TryGetValue("nzdSelected", out bool nzdSelectedValue))
                indicator.nzdSelected = nzdSelectedValue;
            if (settings.TryGetValue("usdSelected", out bool usdSelectedValue))
                indicator.usdSelected = usdSelectedValue;

            if (settings.TryGetValue("timeZoneMode", out int timeZoneModeValue))
                indicator.timeZoneMode = timeZoneModeValue;

            if (settings.TryGetValue("newsPositionX", out int newsPositionXValue))
                indicator.newsPositionX = newsPositionXValue;

            if (settings.TryGetValue("newsPositionY", out int newsPositionYValue))
                indicator.newsPositionY = newsPositionYValue;

            indicator.Refresh();
        }
    }
}
