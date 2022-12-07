using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using System;
using System.Runtime.InteropServices;

namespace LaPalmaTrailsAPI.Tests
{
    public class StatusScraperTests
    {
        const string StatusPageUrl = "Status page.html";
        const string DetailPageWithValidEnglishLink = @"<link rel=""alternate"" hreflang=""en-us"" href=""Link_to_English_version.html"" />";

        private StatusScraper CreateStatusScraper(string testPage)
        {
            StatusScraper scraper = new();
            scraper.StatusPage = testPage;
            scraper.ClearLookups = true;
            scraper.UseCache = false;

            return scraper;
        }

        private static string SimulateWebPage(string bodyContent)
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

        private static string SimulateWebPageWithTableId14(string tableContent)
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

        [Fact]
        public async Task Invalid_status_table_creates_data_error()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPage(@"
                <table id=""tablepress-13"">
                </table>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.DataError.ToString(), scraperResult.Result.Type);
            Assert.Empty(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);

            Assert.Equal(ScraperEvent.EventType.DataError.ToString(), scraperResult.Result.Type);
            Assert.Equal("Trail network probably closed", scraperResult.Result.Message);
            Assert.Equal("Missing table with id tablepress-14", scraperResult.Result.Detail);
        }


        [Fact]
        public async Task Valid_trail_scraped_creates_trail_and_success_result()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Dummy_trail_detail_page.html"">GR 130 Etapa 1</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal("1 additional page lookups", scraperResult.Result.Message);
            Assert.Equal("0 anomalies found", scraperResult.Result.Detail);

            Assert.Single(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);

            TrailStatus trail = scraperResult.Trails[0];
            Assert.Equal("GR 130 Etapa 1", trail.Name);
            Assert.Equal("Open", trail.Status);
            Assert.Equal("Link_to_English_version.html", trail.Url);
        }


        [Fact]
        public async Task Invalid_trail_id_results_in_anomaly()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Detail_page.html"">PR 130 Etapa 1</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Empty(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.UnrecognisedTrailId.ToString(), anomaly.Type);
            Assert.Equal("Unrecognised trail", anomaly.Message);
            Assert.Equal("PR 130 Etapa 1", anomaly.Detail);
        }

        [Fact]
        public async Task Valid_trail_with_extra_zero_after_decimal_point_is_corrected()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Detail_page.html"">PR LP 03.01</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);

            TrailStatus ts = scraperResult.Trails[0];
            Assert.Equal("PR LP 03.1", ts.Name);
            Assert.Equal("Open", ts.Status);
            Assert.Equal("Link_to_English_version.html", ts.Url);
        }


        [Fact]
        public async Task Missing_trail_URL_creates_anomaly()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td>PR LP 01</td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.BadRouteLink.ToString(), anomaly.Type);
            Assert.Equal("PR LP 01", anomaly.Message);
            Assert.Equal("No link to route detail", anomaly.Detail);
        }


        [Fact]
        public async Task Zip_link_creates_anomaly()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Dummy.zip"">PR LP 03</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.BadRouteLink.ToString(), anomaly.Type);
            Assert.Equal("PR LP 03", anomaly.Message);
            Assert.Equal("Dummy.zip", anomaly.Detail);
        }


        [Fact]
        public async Task PDF_link_creates_anomaly()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Dummy.pdf"">PR LP 02</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.BadRouteLink.ToString(), anomaly.Type);
            Assert.Equal("PR LP 02", anomaly.Message);
            Assert.Equal("Dummy.pdf", anomaly.Detail);
        }


        [Fact]
        public async Task Part_open_status_links_to_status_page()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Dummy_trail_detail_page.html"">PR LP 01</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet with additional content</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);

            TrailStatus trail = scraperResult.Trails[0];
            Assert.Equal("PR LP 01", trail.Name);
            Assert.Equal("Part open", trail.Status);
            Assert.Equal(sut.StatusPage, trail.Url);
        }


        [Fact]
        public async Task Uncertain_status_creates_anomaly()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Dummy_trail_detail_page.html"">PR LP 01</a></td>
                    <td>Whatever</td>
                    <td>Blah blah blah</td>
                </tr>                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.UnreadableStatus.ToString(), anomaly.Type);
            Assert.Equal("PR LP 01", anomaly.Message);
            Assert.Equal("Blah blah blah", anomaly.Detail);
        }


        [Fact]
        public async Task Timeout_reading_status_page_creates_timeout_result()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Throws(new TaskCanceledException("Status page timed out"));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Timeout.ToString(), scraperResult.Result.Type);
            Assert.Equal("Status page timed out", scraperResult.Result.Message);
            Assert.Equal(sut.StatusPage, scraperResult.Result.Detail);

            Assert.Empty(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);
        }


        [Fact]
        public async Task Exception_reading_status_page_creates_exception_result()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Throws(new Exception("Random error"));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Exception.ToString(), scraperResult.Result.Type);
            Assert.Equal("Cannot read data", scraperResult.Result.Message);
            Assert.Equal("Random error", scraperResult.Result.Detail);

            Assert.Empty(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);
        }


        // links

        [Fact]
        public async Task English_URL_not_in_map_gets_added_and_returned()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Detail_page.html"">GR 130 Etapa 1</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);
            sut.ClearLookups = false;
            scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Single(scraperResult.Trails);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal("0 additional page lookups", scraperResult.Result.Message);

            TrailStatus trail = scraperResult.Trails[0];
            Assert.Equal("GR 130 Etapa 1", trail.Name);
            Assert.Equal("Open", trail.Status);
            Assert.Equal("Link_to_English_version.html", trail.Url);
        }


        [Fact]
        public async Task English_URL_not_found_creates_anomaly_returns_status_page()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Dummy_trail_detail_page_no_English_link.html"">GR 130 Etapa 1</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Returns(Task.FromResult(@"<link rel=""alternate"" hreflang=""de"" href=""Link_to_German_version.html"" />"));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            TrailStatus trail = scraperResult.Trails[0];
            Assert.Equal(sut.StatusPage, trail.Url);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.BadRouteLink.ToString(), anomaly.Type);
            Assert.Equal("English URL not found", anomaly.Message);
            Assert.Equal("Dummy_trail_detail_page_no_English_link.html", anomaly.Detail);
        }


        [Fact]
        public async Task Timeout_reading_detail_page_creates_anomaly()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Dummy_trail_detail_page.html"">GR 130 Etapa 1</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Throws(new TaskCanceledException("MockWebReader timed out"));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            TrailStatus trail = scraperResult.Trails[0];
            Assert.Equal(sut.StatusPage, trail.Url);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.Timeout.ToString(), anomaly.Type);
            Assert.Equal("MockWebReader timed out", anomaly.Message);
            Assert.Equal("Dummy_trail_detail_page.html", anomaly.Detail);
        }


        [Fact]
        public async Task Exception_reading_detail_page_creates_anomaly()
        {
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            string pageContent = SimulateWebPageWithTableId14(@"
                <tr>
                    <td><a href=""Dummy_trail_detail_page.html"">GR 130 Etapa 1</a></td>
                    <td>Whatever</td>
                    <td>Abierto / Open / Geöffnet</td>
                </tr>
                ");
            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.SetupSequence(x => x.GetStringAsync(It.IsAny<String>()))
                .Returns(Task.FromResult(pageContent))
                .Throws(new Exception("MockWebReader exception"));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient.Object);

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            TrailStatus trail = scraperResult.Trails[0];
            Assert.Equal(sut.StatusPage, trail.Url);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.Exception.ToString(), anomaly.Type);
            Assert.Equal("MockWebReader exception", anomaly.Message);
            Assert.Equal("Dummy_trail_detail_page.html", anomaly.Detail);
        }
    }
}