using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace EconomicEventsIndicator
{
    public static class ForexFactoryScraper
    {
        public static async Task<List<ForexEvent>> GetForexFactoryEvents(string url)
        {
            var httpClient = new HttpClient();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            requestMessage.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            requestMessage.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

            var response = await httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Response status code does not indicate success: {response.StatusCode}");
            }

            var html = await response.Content.ReadAsStringAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var eventsTable = htmlDocument.DocumentNode.SelectSingleNode("//table[contains(@class, 'calendar__table')]");
            if (eventsTable == null)
                return new List<ForexEvent>();

            var rows = eventsTable.SelectNodes(".//tr[contains(@class, 'calendar__row')]");
            if (rows == null)
                return new List<ForexEvent>();

            var events = new List<ForexEvent>();
            string lastTime = null;

            foreach (var row in rows)
            {
                var timeNode = row.SelectSingleNode(".//td[contains(@class, 'calendar__time')]");
                var currencyNode = row.SelectSingleNode(".//td[contains(@class, 'calendar__currency')]");
                var eventNode = row.SelectSingleNode(".//td[contains(@class, 'calendar__event')]");
                var impactNode = row.SelectSingleNode(".//td[contains(@class, 'calendar__impact')]//span");
                var actualNode = row.SelectSingleNode(".//td[contains(@class, 'calendar__actual')]");

                string eventDescription = eventNode?.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(eventDescription))
                    continue;

                string time = timeNode?.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(time))
                    time = lastTime;
                else
                    lastTime = time;

                string result = actualNode?.InnerText.Trim() ?? "";

                EventStatus status = EventStatus.Upcoming;
                if (!string.IsNullOrEmpty(result))
                    status = EventStatus.Completed;

                string impact = "Unknown Impact";
                if (impactNode != null)
                {
                    var impactClass = impactNode.GetAttributeValue("class", "");
                    impact = GetImpactDescription(impactClass);
                }

                var forexEvent = new ForexEvent
                {
                    Time = time,
                    Currency = currencyNode?.InnerText.Trim(),
                    Event = eventDescription,
                    Impact = impact,
                    Status = status,
                    Result = result
                };

                events.Add(forexEvent);
            }

            return events;
        }

        private static string GetImpactDescription(string impactClass)
        {
            if (string.IsNullOrEmpty(impactClass)) return "Unknown Impact";

            if (impactClass.Contains("icon--ff-impact-red")) return "High Impact Expected";
            if (impactClass.Contains("icon--ff-impact-ora")) return "Medium Impact Expected";
            if (impactClass.Contains("icon--ff-impact-yel")) return "Low Impact Expected";
            if (impactClass.Contains("icon--ff-impact-grey")) return "Non-Economic";

            return "Unknown Impact";
        }
    }
}