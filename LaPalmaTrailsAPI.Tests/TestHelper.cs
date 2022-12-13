using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LaPalmaTrailsAPI.Tests
{
    [ExcludeFromCodeCoverage]
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


        // Factory method to create status scraper objects in a 'clean' and reproducable state
        public static StatusScraper CreateStatusScraper(string testPage = StatusPageUrl, bool clearLookups = true, bool useCache = false)
        {
            StatusScraper scraper = new();
            scraper.StatusPage = testPage;
            scraper.ClearLookups = clearLookups;
            scraper.UseCache = useCache;

            return scraper;
        }

        // Minimal valid web page content creator
        public static string StatusPage(string bodyContent)
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
        public static string StatusPageWithValidTable(string tableContent)
        {
            return StatusPage($@"
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

        // Creates a web page with a valid table with a single row containing a valid open trail
        public static string StatusPageWithWithSingleValidOpenTrail = StatusPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

        public static void DeleteLookupTableFile()
        {
            if (File.Exists(CachedUrlLookupTable.UrlTableLookupFileName))
            {
                File.Delete(CachedUrlLookupTable.UrlTableLookupFileName);
            }
        }

        public static void CreateLookupTableFile(string contents)
        {
            File.WriteAllText(CachedUrlLookupTable.UrlTableLookupFileName, contents);
        }



    }
}
