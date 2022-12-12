using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LaPalmaTrailsAPI.Tests
{
    internal static class TestHelper
    {
        // Test data
        public const string StatusPageUrl = "Status page.html";
        public const string LinkToValidDetailPage = "Detail_page.html";
        public const string IgnoredContent = "Whatever";
        public const string LinkToEnglishVersion = "Link_to_English_version.html";
        public const string DetailPageWithValidEnglishLink = $@"<link rel=""alternate"" hreflang=""en-us"" href={LinkToEnglishVersion} />";

        // Expected text for open trails
        public const string TrailOpen = "Abierto / Open / Geöffnet";


        //
        // Helper setup methods
        //


        // Factory method to create status scraper objects in a 'clean' and reproducable state
        public static StatusScraper CreateStatusScraper(string testPage)
        {
            StatusScraper scraper = new();
            scraper.StatusPage = testPage;
            scraper.ClearLookups = true;
            scraper.UseCache = false;

            return scraper;
        }

        // Minimal valid web page content creator
        public static string SimulateWebPage(string bodyContent)
        {
            return $@"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                  <meta charset=""UTF-8"">
                  <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
                  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                  <title>Test 1</title>
                </head>
                <body>
                    {bodyContent}
                </body>
                </html>";
        }

        // Creates a web page with table of correct id to scrape
        public static string SimulateWebPageWithValidTable(string tableContent)
        {
            return SimulateWebPage($@"
                <table id=""tablepress-14"">
                  <thead>
                    <tr>
                      <th>Route</th>
                      <th>Other stuff</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tableContent}
                  </tbody>
                </table>");
        }

    }
}
