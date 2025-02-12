using System;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace EconomicEventsIndicator
{
    public partial class EconomicEventsIndicator
    {
        public override IList<SettingItem> Settings
        {
            get
            {
                var settings = base.Settings;

                var chartDateSelector = new SelectItem("Current Chart Date", 1);
                var customDateSelector = new SelectItem("Custom Date", 2);

                AddSettingWithConfirmation(settings,
                    new SettingItemSelectorLocalized("dateMode",
                    new SelectItem("dateMode", dateMode),
                    [
                        chartDateSelector,
                        customDateSelector
                    ])
                    {
                        Text = "Select Date:"
                    });

                AddSettingWithConfirmation(settings, new SettingItemDateTime("customStartDate", customStartDate)
                {
                    Text = "From Date",
                    Relation = new SettingItemRelationVisibility("dateMode", customDateSelector)
                });

                AddSettingWithConfirmation(settings, new SettingItemDateTime("customEndDate", customEndDate)
                {
                    Text = "To Date",
                    Relation = new SettingItemRelationVisibility("dateMode", customDateSelector)
                });

                // Impact Settings
                AddSettingWithConfirmation(settings, new SettingItemBoolean("highImpact", highImpact)
                {
                    Text = "High Impact"
                });

                AddSettingWithConfirmation(settings, new SettingItemBoolean("mediumImpact", mediumImpact)
                {
                    Text = "Medium Impact"
                });

                AddSettingWithConfirmation(settings, new SettingItemBoolean("lowImpact", lowImpact)
                {
                    Text = "Low Impact"
                });

                AddSettingWithConfirmation(settings, new SettingItemBoolean("nonEconomicNews", nonEconomicNews)
                {
                    Text = "Non-Economic Events"
                });

                // Currency Settings
                var allCurrenciesSelector = new SelectItem("All", 1);
                var selectCurrencySelector = new SelectItem("Select Currency", 2);

                AddSettingWithConfirmation(settings,
                    new SettingItemSelectorLocalized("currencyMode",
                    new SelectItem("currencyMode", currencyMode),
                    [
                        allCurrenciesSelector,
                        selectCurrencySelector
                    ])
                    {
                        Text = "Currency:"
                    });

                var currencySettings = new (string name, string text, bool value)[]
                {
                    ("usdSelected", "USD", usdSelected),
                    ("eurSelected", "EUR", eurSelected),
                    ("audSelected", "AUD", audSelected),
                    ("cadSelected", "CAD", cadSelected),
                    ("chfSelected", "CHF", chfSelected),
                    ("cnySelected", "CNY", cnySelected),
                    ("gbpSelected", "GBP", gbpSelected),
                    ("jpySelected", "JPY", jpySelected),
                    ("nzdSelected", "NZD", nzdSelected)
                };

                foreach (var currency in currencySettings)
                {
                    AddSettingWithConfirmation(settings, new SettingItemBoolean(currency.name, currency.value)
                    {
                        Text = currency.text,
                        Relation = new SettingItemRelationVisibility("currencyMode", selectCurrencySelector)
                    });
                }

                // Timezone Settings
                AddSettingWithConfirmation(settings,
                    new SettingItemSelectorLocalized("timeZoneMode",
                    new SelectItem("timeZoneMode", timeZoneMode),
                    [
                        new SelectItem("Current Chart Timezone", 1),
                        new SelectItem("System Timezone", 2)
                    ])
                    {
                        Text = "Time Zone:"
                    });

                // Position Settings
                AddSettingWithConfirmation(settings, new SettingItemInteger("newsPositionX", newsPositionX)
                {
                    Text = "Move Left/Right (-/+)"
                });

                AddSettingWithConfirmation(settings, new SettingItemInteger("newsPositionY", newsPositionY)
                {
                    Text = "Move Up/Down (-/+)"
                });

                return settings;
            }
            set
            {
                if (value.TryGetValue("dateMode", out int dateModeValue))
                    dateMode = dateModeValue;

                if (value.TryGetValue("customStartDate", out DateTime customStartDateValue))
                    customStartDate = customStartDateValue;

                if (value.TryGetValue("customEndDate", out DateTime customEndDateValue))
                    customEndDate = customEndDateValue;

                if (value.TryGetValue("highImpact", out bool highImpactValue))
                    highImpact = highImpactValue;

                if (value.TryGetValue("mediumImpact", out bool mediumImpactValue))
                    mediumImpact = mediumImpactValue;

                if (value.TryGetValue("lowImpact", out bool lowImpactValue))
                    lowImpact = lowImpactValue;

                if (value.TryGetValue("nonEconomicNews", out bool nonEconomicNewsValue))
                    nonEconomicNews = nonEconomicNewsValue;

                if (value.TryGetValue("currencyMode", out int currencyModeValue))
                    currencyMode = currencyModeValue;

                if (value.TryGetValue("audSelected", out bool audSelectedValue))
                    audSelected = audSelectedValue;

                if (value.TryGetValue("cadSelected", out bool cadSelectedValue))
                    cadSelected = cadSelectedValue;

                if (value.TryGetValue("chfSelected", out bool chfSelectedValue))
                    chfSelected = chfSelectedValue;

                if (value.TryGetValue("cnySelected", out bool cnySelectedValue))
                    cnySelected = cnySelectedValue;

                if (value.TryGetValue("eurSelected", out bool eurSelectedValue))
                    eurSelected = eurSelectedValue;

                if (value.TryGetValue("gbpSelected", out bool gbpSelectedValue))
                    gbpSelected = gbpSelectedValue;

                if (value.TryGetValue("jpySelected", out bool jpySelectedValue))
                    jpySelected = jpySelectedValue;

                if (value.TryGetValue("nzdSelected", out bool nzdSelectedValue))
                    nzdSelected = nzdSelectedValue;

                if (value.TryGetValue("usdSelected", out bool usdSelectedValue))
                    usdSelected = usdSelectedValue;

                if (value.TryGetValue("timeZoneMode", out int timeZoneModeValue))
                {
                    timeZoneMode = timeZoneModeValue;
                    CurrentChart.Refresh();
                }

                if (value.TryGetValue("newsPositionX", out int newsPositionXValue))
                    newsPositionX = newsPositionXValue;

                if (value.TryGetValue("newsPositionY", out int newsPositionYValue))
                    newsPositionY = newsPositionYValue;


                Refresh();
            }
        }

        private void AddSettingWithConfirmation(IList<SettingItem> settings, SettingItem settingItem)
        {
            settingItem.ValueChangingBehavior = SettingItemValueChangingBehavior.WithConfirmation;
            settings.Add(settingItem);
        }
    }
}