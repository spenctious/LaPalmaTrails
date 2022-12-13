using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Reflection.PortableExecutable;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace LaPalmaTrailsAPI
{
    /// <summary>
    /// Performs web scraping of the official trail status page for La Palma.
    /// 
    /// Records:
    /// - DateTime of when the site was scraped
    /// - Statuses of all the recognised trails
    /// - A collection of anomalies encountered while scraping the site
    /// - An overall result
    /// 
    /// Successful scraping results are cached and refreshed if over a day old.
    /// </summary>
    public class StatusScraper : IStatusScraper
    {
        // A thread-safe lookup table for converting Spanish URLs to English equivalents
        private static PersistentLookupTable _urlMap = new();

        /// <summary>
        /// Properties that can be defined by optional API call parameters
        /// </summary>
        public string StatusPage { get; set; } = "https://www.senderosdelapalma.es/en/footpaths/situation-of-the-footpaths/"; // site to scrape
        public int StatusPageTimeout { get; set; } = 5000; // ms
        public int DetailPageTimeout { get; set; } = 5000; // ms
        public bool UseCache { get; set; } = true; // set false to guarantee a fresh result
        public bool ClearLookups { get; set; } = false; // build the lookup table fresh each time


        public static bool LoadUrlLookupTable()
        {
            return _urlMap.LoadUrlLookupTable();
        }


        private string GetTrailId(HtmlNode row, ScraperResult scraperResult)
        {
            string trailId = "Unrecognised trail"; // default

            var trail = row.SelectSingleNode("td[position()=1]").InnerText;
            var match = TrailScraperRegex.MatchValidTrailFormats(trail);

            if (match.Success)
            {
                trailId = match.ToString();

                // fix the website error where some trails are misnamed with an extra leading zero
                if (TrailScraperRegex.TrailIdHasTwoDigitsAfterDecimal(trailId))
                {
                    trailId = trailId.Remove(trailId.Length - 2, 1);
                }
            }
            else
            {
                // trail does not match any recognised trail pattern
                scraperResult.AddAnomaly(ScraperEvent.EventType.UnrecognisedTrailId, trailId, trail.Trim());
            }

            return trailId;
        }


        private string GetSpanishDetailLink(HtmlNode row, string trailId, ScraperResult scraperResult)
        {
            var link = row.SelectSingleNode("td[position()=1]//a[@href]");
            string scrapedUrl = link == null ? "failed" : link.GetAttributeValue("href", "failed");

            if (scrapedUrl == "failed")
            {
                // the trail ID column doesn't appear to have a valid link
                scraperResult.AddAnomaly(ScraperEvent.EventType.BadRouteLink, trailId, "No link to route detail");
                scrapedUrl = StatusPage;
            }
            else
            {
                // we don't want to link to large PDFs or inappropriate GPX files in ZIP format
                // so skip and use the default
                if (scrapedUrl.EndsWith(".pdf") || scrapedUrl.EndsWith(".zip"))
                {
                    scraperResult.AddAnomaly(ScraperEvent.EventType.BadRouteLink, trailId, scrapedUrl);
                    scrapedUrl = StatusPage;
                }
            }

            return scrapedUrl;
        }


        private string GetTrailStatus(HtmlNode row, string trailId, ScraperResult scraperResult)
        {
            string trailStatus = "Unknown"; // default

            var status = row.SelectSingleNode("td[position()=3]").InnerText;
            if (TrailScraperRegex.TrailIsOpen(status))
            {
                if (TrailScraperRegex.TrailIsCompletelyOpen(status))
                {
                    trailStatus = "Open";
                }
                else
                {
                    trailStatus = "Part open";
                }
            }
            else if (TrailScraperRegex.TrailIsClosed(status))
            {
                trailStatus = "Closed";
            }
            else
            {
                // trail status isn't clearly marked as open or closed
                scraperResult.AddAnomaly(ScraperEvent.EventType.UnreadableStatus, trailId, status);
            }

            return trailStatus;
        }


        /// <summary>
        /// Reads trail status information from the official trail website.
        /// </summary>
        /// <returns>A ScraperResult object that records the results of the scraping operation.</returns>
        public async Task<ScraperResult> GetTrailStatuses(IHttpClient httpClient)
        {
            // clear lookup table if asked to
            if (ClearLookups)
            {
                _urlMap.Clear();
            }
            int initialUrlMapCount = _urlMap.Count;

            // use the cached result if it's still valid
            if (LastResultIsStillValid)
            {
                return CachedResult.Instance.Value;
            }


            // scrape the status page
            ScraperResult scraperResult = new();
            var doc = new HtmlDocument();
            try
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(StatusPageTimeout);
                var html = await httpClient.GetStringAsync(StatusPage);
                doc.LoadHtml(html);


                // ********** Find the trail status table

                // extract the table rows that are not header rows and make sure we have a table to parse:
                // in cases of general trail shutdown (fire alerts etc.) the table may be missing
                var nodes = doc.DocumentNode.SelectNodes("//table[@id='tablepress-14']//tr[not(th)]");
                if (nodes == null)
                {
                    scraperResult.DataError("Trail network probably closed", "Missing table with id tablepress-14");
                    throw new InvalidOperationException();
                }

                // ********** Parse the table row by row

                foreach (HtmlNode row in nodes)
                {
                    // get trail id
                    string trailId = GetTrailId(row, scraperResult);

                    // get detail page link
                    string detailPageInSpanish = GetSpanishDetailLink(row, trailId, scraperResult);
                    string trailUrl = StatusPage;
                    if (detailPageInSpanish != StatusPage)
                    {
                        trailUrl = await GetEnglishUrl(httpClient, trailId, detailPageInSpanish, scraperResult);
                    }

                    // get status
                    string trailStatus = GetTrailStatus(row, trailId, scraperResult);
                    var status = row.SelectSingleNode("td[position()=3]").InnerText;
                    if (trailStatus == "Part open")
                    {
                        trailUrl = StatusPage; // link to the status page as this is where further details can be found
                    }

                    // add valid trails to trail list
                    if (trailId != "Unrecognised trail")
                    {
                        scraperResult.AddTrailStatus(trailId, trailStatus, trailUrl);
                    }
                }

                // ********** Status page successfully scraped

                // if we found additional links, update the file version of the lookup table
                int additionalLookups = _urlMap.Count - initialUrlMapCount;
                if (additionalLookups > 0)
                {
                    _urlMap.SaveUrlLookupTable();
                }

                scraperResult.Success($"{additionalLookups} additional page lookups", $"{scraperResult.Anomalies.Count} anomalies found");

                // update cache
                CachedResult.Instance.Value = scraperResult;
            }
            catch (InvalidOperationException)
            {
                // scraper result should be set before throw for specific circumstances
            }
            catch (TaskCanceledException ex)
            {
                // handle timeouts
                scraperResult.Timeout(ex.Message, StatusPage);
            }
            catch (Exception ex)
            {
                scraperResult.Exception("Cannot read data", ex.Message);
            }

            return scraperResult;
        }



        /// <summary>
        /// Scrapes the Spanish route detail page to extract the link to the English language version of the page
        /// </summary>
        /// <param name="routeId">The route ID already scraped - used for anomaly recording purposes only.</param>
        /// <param name="spanishUrl">The link to scrape for the English version.</param>
        /// <param name="scraperResult">The ScraperResult to which any anomalies found are to be added.</param>
        /// <returns>The link to the English language version of the page or the trail status page if not found.</returns>
        public async Task<string> GetEnglishUrl(IHttpClient webReader, string routeId, string spanishUrl, ScraperResult scraperResult)
        {
            string detailLink = StatusPage;

            // if we already have an English version get it, otherwise try to scrape it
            if (_urlMap.ContainsKey(spanishUrl))
            {
                detailLink = _urlMap.GetValue(spanishUrl);
            }
            else
            {
                var doc = new HtmlDocument();
                try
                {
                    // get the html page source 
                    webReader.Timeout = TimeSpan.FromMilliseconds(DetailPageTimeout);
                    var html = await webReader.GetStringAsync(spanishUrl);
                    doc.LoadHtml(html);

                    // look for the link to the English language version of the page
                    var link = doc.DocumentNode.SelectSingleNode("//link[@rel='alternate'][@hreflang='en-us']");

                    string englishUrl = link == null ? "failed" : link.GetAttributeValue("href", "failed");
                    if (englishUrl == "failed")
                    {
                        scraperResult.AddAnomaly(ScraperEvent.EventType.BadRouteLink, "English URL not found", spanishUrl);
                    }
                    else
                    {
                        _urlMap.TryAdd(spanishUrl, englishUrl); // ignore result - if key is already there then the value should be the same
                        detailLink = englishUrl;
                    }
                }
                // lookup failures are not fatal so record them as anomalies
                catch (TaskCanceledException ex)
                {
                    scraperResult.AddAnomaly(ScraperEvent.EventType.Timeout, ex.Message, spanishUrl);
                }
                catch (Exception ex)
                {
                    scraperResult.AddAnomaly(ScraperEvent.EventType.Exception, ex.Message, spanishUrl);
                }
            }

            return detailLink;
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
