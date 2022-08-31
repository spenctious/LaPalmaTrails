using HtmlAgilityPack;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace lpfwAPI // concurrent
{
    public class StatusScraper
    {
        // A thread-safe lookup table for converting Spanish URLs to English equivalents
        private static ConcurrentDictionary<string, string> _urlMap = new();

        public string StatusPage { get; set; } = "https://www.senderosdelapalma.es/en/footpaths/situation-of-the-footpaths/"; // site to scrape
        public int StatusPageTimeout { get; set; } = 6000; // ms
        public int DetailPageTimeout { get; set; } = 20000; // ms
        public bool UseCache { get; set; } = true; // 
        public bool ClearLookups { get; set; } = false; // build the lookup table fresh each time


        /// <summary>
        /// Scrapes the official trail status page for trail status information.
        /// Returns cached results if the site was scraped less than a day ago.
        /// </summary>
        /// <returns>A ScraperResult object populated with the results of the scraping operation</returns>
        public async Task<ScraperResult> GetTrailStatuses()
        {
            ScraperResult scraperResult = new();

            if (ClearLookups) _urlMap.Clear();
            int initialMapCount = _urlMap.Count;

            if (LastResultIsStillValid)
            {
                return CachedResult.Instance.Value;
            }

            var doc = new HtmlDocument();
            try
            {
                // get the html page source 
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(StatusPageTimeout);
                    var html = await httpClient.GetStringAsync(StatusPage);
                    doc.LoadHtml(html);
                }

                // extract the table rows that are not header rows
                var nodes = doc.DocumentNode.SelectNodes("//table[@id='tablepress-14']//tr[not(th)]");
                if (nodes == null)
                {
                    // in cases of general trail shutdown (fire alerts etc.) the table may be missing
                    scraperResult.RecordFatalError(
                        ScraperEvent.EventType.DataError, 
                        "Trail network probably closed", 
                        "Missing table with id tablepress-14");
                }
                else
                {
                    // process table rows concurrently since they may require their own slow page lookups
                    // wait for all the results to come in before proceeding
                    List<Task> taskList = new();
                    foreach(var tableRow in nodes)
                    {
                        Task runningTask = Task.Run(() => AddTrailInformation(tableRow, scraperResult));
                        taskList.Add(runningTask);
                    }
                    Task.WaitAll(taskList.ToArray());

                    // successful scraping
                    scraperResult.Result.Type       = ScraperEvent.EventType.Success.ToString();
                    scraperResult.Result.Message    = $"{_urlMap.Count - initialMapCount} additional page lookups";
                    scraperResult.Result.Detail     = $"{scraperResult.Anomalies.Count} anomalies found";
                    scraperResult.LastScraped       = DateTime.Now;

                    // cache result
                    CachedResult.Instance.Value = scraperResult;
                }
            }
            catch (TaskCanceledException ex)
            {
                // handle timeouts
                scraperResult.RecordFatalError(
                    ScraperEvent.EventType.Timeout,
                    ex.Message,
                    StatusPage);
            }
            catch (Exception ex)
            {
                scraperResult.RecordFatalError(
                    ScraperEvent.EventType.Exception,
                    "Cannot read data",
                    ex.Message);
            }

            return scraperResult;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="tableRow"></param>
        /// <param name="scraperResult"></param>
        /// <returns></returns>
        private async Task AddTrailInformation(HtmlNode tableRow, ScraperResult scraperResult)
        {
            // set defaults
            string trailId = "Unrecognised trail";
            string trailStatus = "Unknown";

            // get the trail id - first table column
            var trail = tableRow.SelectSingleNode("td[position()=1]").InnerText;
            var match = Regex.Match(trail, @"(GR 13(0|1) Etapa \d)|((PR|SL) [A-Z][A-Z] \d{2,3}(\.0\d|\.\d|)?)");
            if (match.Success)
            {
                trailId = match.ToString();

                // fix the website error where some trails are misnamed with an extra leading zero
                if (Regex.IsMatch(trailId, @"\.\d\d$"))
                {
                    trailId = trailId.Remove(trailId.Length - 2, 1);
                }
            }
            else
            {
                // trail does not match any recognised trail pattern
                scraperResult.AddAnomaly(ScraperEvent.EventType.UnrecognisedTrailId, trailId, trail.Trim());
            }


            // attempt to retrieve the English language version of the route page
            // if unsuccessful just link back to the status page
            string trailUrl = StatusPage; // default
            var link = tableRow.SelectSingleNode("td[position()=1]//a[@href]");
            if (link == null)
            {
                scraperResult.AddAnomaly(ScraperEvent.EventType.BadRouteLink, trailId, "No link to route detail");
            }
            else
            {
                string scrapedUrl = link.GetAttributeValue("href", "failed");
                if (scrapedUrl == "failed")
                {
                    // revert to default and report
                    scraperResult.AddAnomaly(ScraperEvent.EventType.BadRouteLink, trailId, scrapedUrl);
                }
                else
                {
                    trailUrl = await GetEnglishUrl(trailId, scrapedUrl, scraperResult);
                }
            }


            // get the trail status - 3rd table column:
            // N.B. some entries have additional line breaks that have to be catered for
            const string openPattern = "Abierto / Open / Geöffnet";
            const string closedPattern = "Cerrado / Closed / Gesperrt";
            const string completelyOpenPattern = @"^Abierto / Open / Geöffnet(<br />)?$";

            var status = tableRow.SelectSingleNode("td[position()=3]").InnerText;
            if (Regex.IsMatch(status, openPattern))
            {
                if (Regex.IsMatch(status, completelyOpenPattern))
                {
                    trailStatus = "Open";
                }
                else
                {
                    // more in the column than just a statement that it is open - means some sort of qualification
                    trailStatus = "Part open";
                }
            }
            else if (Regex.IsMatch(status, closedPattern))
            {
                trailStatus = "Closed";
            }
            else
            {
                // trail status isn't clearly marked as open or closed - default 'Unknown' used
                scraperResult.AddAnomaly(ScraperEvent.EventType.UnreadableStatus, trailId, status);
            }

            // only add valid trails
            if (trailId != "Unrecognised trail")
            {
                scraperResult.AddTrailStatus(trailId, trailStatus, trailUrl);
            }
        }


        /// <summary>
        /// Provides an English Language equivalent link from the given Spanish link or the trail status lookup page link if not available.
        /// If no value is found in the local cache the link will be sourced from the Spanish page and added to the cache.
        /// PDF and ZIP file links are not suitable for return so are substituted with the trail status page.
        /// </summary>
        /// <param name="rowIndex">Row number in the trail status page</param>
        /// <param name="routeId">The already parsed route ID</param>
        /// <param name="spanishUrl">The original link to the Spanish page</param>
        /// <param name="scraperResult">The scraper result object to which any anomalies found will be added.</param>
        /// <returns>The most appropriate trail link based on the given Spanish URL</returns>
        private async Task<string> GetEnglishUrl(string routeId, string spanishUrl, ScraperResult scraperResult)
        {
            // we don't want to link to large PDFs or inappropriate GPX files in ZIP format - return default
            if (spanishUrl.EndsWith(".pdf") || spanishUrl.EndsWith(".zip"))
            {
                scraperResult.AddAnomaly(ScraperEvent.EventType.BadRouteLink, routeId, spanishUrl);

                return StatusPage;
            }

            // if we already have an English version return it
            if (_urlMap.ContainsKey(spanishUrl))
            {
                return _urlMap.GetValueOrDefault(spanishUrl, StatusPage);
            }


            // no English entry in dictionary yet so try to read it from the Spanish URL
            string englishUrl = StatusPage;
            var doc = new HtmlDocument();
            try
            {
                // get the html page source 
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMilliseconds(DetailPageTimeout);
                    var html = await httpClient.GetStringAsync(spanishUrl);

                    // get the page as a HTML document
                    doc.LoadHtml(html);

                    // look for the link to the English language version of thepage
                    var linkNode = doc.DocumentNode.SelectSingleNode("//link[@rel='alternate'][@hreflang='en-us']");
                    string link = linkNode.GetAttributeValue("href", "failed");

                    // add link to dictionary or log it as an anomaly if not found
                    if (link == "failed")
                    {
                        scraperResult.AddAnomaly(ScraperEvent.EventType.BadRouteLink, "English URL not found",spanishUrl);
                    }
                    else
                    {
                        // success - add to dictionary 
                        englishUrl = link;
                        if (!_urlMap.TryAdd(spanishUrl, englishUrl))
                        {
                            scraperResult.AddAnomaly(ScraperEvent.EventType.Unexpected, "Couldn't add to dictionary or already there", spanishUrl);
                        }
                    }
                }
            }
            // lookup failures are not fatal so record them as anomalies
            catch (TaskCanceledException ex)
            {
                scraperResult.AddAnomaly(ScraperEvent.EventType.Timeout, ex.Message, spanishUrl);
            }
            catch (Exception ex)
            {
                scraperResult.AddAnomaly(
                    ScraperEvent.EventType.Exception, 
                    ex.Message,
                    spanishUrl);
            }

            return englishUrl;
        }


        /// <summary>
        /// Returns true if the site was successfully scraped within the last day, false otherwise
        /// </summary>
        private bool LastResultIsStillValid
        {
            get
            {
                if (!UseCache) return false;

                TimeSpan span = DateTime.Now.Subtract(CachedResult.Instance.Value.LastScraped);
                return (int)span.TotalDays < 1;
            }
        }
    }
}
