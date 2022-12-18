using Castle.Components.DictionaryAdapter.Xml;
using FluentAssertions;
using FluentAssertions.Execution;
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
        #region TEST DATA

        // Test data
        public const string StatusPageUrl = "Status page.html";
        public const string LinkToValidDetailPage = "Detail_page.html";
        public const string LinkToEnglishVersion = "Link_to_English_version.html";
        public const string DetailPageWithValidEnglishLink = $@"<link rel=""alternate"" hreflang=""en-us"" href={LinkToEnglishVersion} />";

        // Expected text for open trails
        public const string TrailOpen = "Abierto / Open / Geöffnet";

        // Shorthand for certain expected values
        public static readonly TrailStatus Gr130_Open_EnglishLink = new TrailStatus(
            "GR 130 Etapa 1", 
            StatusScraper.Status.Open, 
            LinkToEnglishVersion);

        public static readonly ScraperEvent SuccessResult_OneLookup_NoAnomalies = new ScraperEvent(
            ScraperEvent.EventType.Success, 
            "1 additional page lookups", 
            "0 anomalies found");

        #endregion

        #region FACTORY METHODS

        public static StatusScraper CreateStatusScraper(string testPage = StatusPageUrl, bool clearLookups = true, bool useCache = false)
        {
            StatusScraper scraper = new();
            scraper.StatusPage = testPage;
            scraper.ClearLookups = clearLookups;
            scraper.UseCache = useCache;

            return scraper;
        }

        #endregion

        #region STATUS PAGE GENERATORS

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
        public static string StatusPageWithWithSingleOpenGr130ValidDetailLink = StatusPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>ignored content</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

        #endregion

        #region FILE METHODS

        // deletes the file used for url lookup persistence
        public static void DeleteLookupTableFile()
        {
            if (File.Exists(CachedUrlLookupTable.UrlTableLookupFileName))
            {
                File.Delete(CachedUrlLookupTable.UrlTableLookupFileName);
            }
        }


        // creates a url lookup table file
        public static void CreateLookupTableFile(string contents)
        {
            File.WriteAllText(CachedUrlLookupTable.UrlTableLookupFileName, contents);
        }

        #endregion

        #region ASSERTION EXTENSION METHODS

        public static void ShouldMatch(this ScraperEvent actual, ScraperEvent expected)
        {
            using (new AssertionScope())
            {
                actual.Type.Should().Be(expected.Type);
                actual.Message.Should().Be(expected.Message);
                actual.Detail.Should().Be(expected.Detail);
            }
        }


        public static void ShouldMatch(this TrailStatus actual, TrailStatus expected)
        {
            using (new AssertionScope())
            {
                actual.Name.Should().Be(expected.Name);
                actual.Status.Should().Be(expected.Status);
                actual.Url.Should().Be(expected.Url);
            }
        }

        #endregion
    }
}
