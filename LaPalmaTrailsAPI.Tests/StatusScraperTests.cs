using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace LaPalmaTrailsAPI.Tests
{
    [ExcludeFromCodeCoverage]
    public class StatusScraperTests
    {
        // Test data
        const string StatusPageUrl = "Status page.html";
        const string LinkToValidDetailPage = "Detail_page.html";
        const string IgnoredContent = "Whatever";
        const string LinkToEnglishVersion = "Link_to_English_version.html";
        const string DetailPageWithValidEnglishLink = $@"<link rel=""alternate"" hreflang=""en-us"" href={LinkToEnglishVersion} />";

        // Expected text for open trails
        const string TrailOpen = "Abierto / Open / Geöffnet";


        //
        // Helper setup methods
        //


        // Factory method to create status scraper objects in a 'clean' and reproducable state
        private StatusScraper CreateStatusScraper(string testPage)
        {
            StatusScraper scraper = new();
            scraper.StatusPage = testPage;
            scraper.ClearLookups = true;
            scraper.UseCache = false;

            return scraper;
        }

        // Minimal valid web page content creator
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

        // Creates a web page with table of correct id to scrape
        private static string SimulateWebPageWithValidTable(string tableContent)
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


        //
        // Tests
        //

        [Fact]
        public async Task Invalid_status_table_creates_data_error()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPage(@"
                <table id=""tablepress-13"">
                </table>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(Task.FromResult(pageContent));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.DataError.ToString());
                scraperResult.Result.Message.Should().Be("Trail network probably closed");
                scraperResult.Result.Detail.Should().Be("Missing table with id tablepress-14");
            }
            scraperResult.Trails.Should().BeEmpty();
            scraperResult.Anomalies.Should().BeEmpty();
        }


        [Fact]
        public async Task Valid_trail_scraped_creates_trail_and_success_result()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
                scraperResult.Result.Message.Should().Be("1 additional page lookups");
                scraperResult.Result.Detail.Should().Be("0 anomalies found");
            }
            scraperResult.Anomalies.Should().BeEmpty();
            scraperResult.Trails.Should().HaveCount(1);

            using (new AssertionScope())
            {
                TrailStatus trail = scraperResult.Trails[0];
                trail.Name.Should().Be("GR 130 Etapa 1");
                trail.Status.Should().Be("Open");
                trail.Url.Should().Be("Link_to_English_version.html");
            }
        }


        [Fact]
        public async Task Invalid_trail_id_results_in_anomaly()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>PR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Trails.Should().BeEmpty();
            scraperResult.Anomalies.Should().HaveCount(1);

            using (new AssertionScope())
            {
                ScraperEvent anomaly = scraperResult.Anomalies[0];
                anomaly.Type.Should().Be(ScraperEvent.EventType.UnrecognisedTrailId.ToString());
                anomaly.Message.Should().Be("Unrecognised trail");
                anomaly.Detail.Should().Be("PR 130 Etapa 1");
            }
        }


        [Fact]
        public async Task Valid_trail_with_extra_zero_after_decimal_point_is_corrected()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>PR LP 03.01</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Anomalies.Should().BeEmpty();
            scraperResult.Trails.Should().HaveCount(1);

            using (new AssertionScope())
            {
                TrailStatus trail = scraperResult.Trails[0];
                trail.Name.Should().Be("PR LP 03.1");
                trail.Status.Should().Be("Open");
                trail.Url.Should().Be(LinkToEnglishVersion);
            }
        }


        [Fact]
        public async Task Missing_trail_URL_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td>PR LP 01</td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().HaveCount(1);

            using (new AssertionScope())
            {
                ScraperEvent anomaly = scraperResult.Anomalies[0];
                anomaly.Type.Should().Be(ScraperEvent.EventType.BadRouteLink.ToString());
                anomaly.Message.Should().Be("PR LP 01");
                anomaly.Detail.Should().Be("No link to route detail");
            }
        }


        [Fact]
        public async Task Zip_link_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href=""Dummy.zip"">PR LP 03</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().HaveCount(1);

            using (new AssertionScope())
            {
                ScraperEvent anomaly = scraperResult.Anomalies[0];
                anomaly.Type.Should().Be(ScraperEvent.EventType.BadRouteLink.ToString());
                anomaly.Message.Should().Be("PR LP 03");
                anomaly.Detail.Should().Be("Dummy.zip");
            }
        }


        [Fact]
        public async Task PDF_link_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href=""Dummy.pdf"">PR LP 02</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().HaveCount(1);

            using (new AssertionScope())
            {
                ScraperEvent anomaly = scraperResult.Anomalies[0];
                anomaly.Type.Should().Be(ScraperEvent.EventType.BadRouteLink.ToString());
                anomaly.Message.Should().Be("PR LP 02");
                anomaly.Detail.Should().Be("Dummy.pdf");
            }
        }


        [Fact]
        public async Task Part_open_status_links_to_status_page()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>PR LP 01</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen} with additional content</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Anomalies.Should().BeEmpty();
            scraperResult.Trails.Should().HaveCount(1);

            using (new AssertionScope())
            {
                TrailStatus trail = scraperResult.Trails[0];
                trail.Name.Should().Be("PR LP 01");
                trail.Status.Should().Be("Part open");
                trail.Url.Should().Be(sut.StatusPage);
            }
        }


        [Fact]
        public async Task Uncertain_status_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>PR LP 01</a></td>
                    <td>{IgnoredContent}</td>
                    <td>Blah blah blah</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().HaveCount(1);

            using (new AssertionScope())
            {
                ScraperEvent anomaly = scraperResult.Anomalies[0];
                anomaly.Type.Should().Be(ScraperEvent.EventType.UnreadableStatus.ToString());
                anomaly.Message.Should().Be("PR LP 01");
                anomaly.Detail.Should().Be("Blah blah blah");
            }
        }


        [Fact]
        public async Task Timeout_reading_status_page_creates_timeout_result()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>())
                .Returns(Task.FromException<string>(new TaskCanceledException("Status page timed out")));
            
            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Timeout.ToString());
                scraperResult.Result.Message.Should().Be("Status page timed out");
                scraperResult.Result.Detail.Should().Be(sut.StatusPage);
            }
            scraperResult.Anomalies.Should().BeEmpty();
            scraperResult.Trails.Should().BeEmpty();
        }


        [Fact]
        public async Task Exception_reading_status_page_creates_exception_result()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>())
                .Returns(Task.FromException<string>(new Exception("Random error")));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Exception.ToString());
                scraperResult.Result.Message.Should().Be("Cannot read data");
                scraperResult.Result.Detail.Should().Be("Random error");
            }
            scraperResult.Anomalies.Should().BeEmpty();
            scraperResult.Trails.Should().BeEmpty();
        }


        [Fact]
        public async Task English_URL_not_in_map_gets_added_and_returned()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink),
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);
            sut.ClearLookups = false;
            scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
                scraperResult.Result.Message.Should().Be("0 additional page lookups");
            }
            scraperResult.Trails.Should().HaveCount(1);

            using (new AssertionScope())
            {
                TrailStatus trail = scraperResult.Trails[0];
                trail.Name.Should().Be("GR 130 Etapa 1");
                trail.Status.Should().Be("Open");
                trail.Url.Should().Be(LinkToEnglishVersion);
            }
        }


        [Fact]
        public async Task English_URL_not_found_creates_anomaly_returns_status_page()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href=""Detail_page_no_English_link.html"">GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(@"<link rel=""alternate"" hreflang=""de"" href=""Link_to_German_version.html"" />"));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().HaveCount(1);

            TrailStatus trail = scraperResult.Trails[0];
            trail.Url.Should().Be(sut.StatusPage);

            using (new AssertionScope())
            {
                ScraperEvent anomaly = scraperResult.Anomalies[0];
                anomaly.Type.Should().Be(ScraperEvent.EventType.BadRouteLink.ToString());
                anomaly.Message.Should().Be("English URL not found");
                anomaly.Detail.Should().Be("Detail_page_no_English_link.html");
            }
        }


        [Fact]
        public async Task Timeout_reading_detail_page_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                x => Task.FromResult(pageContent),
                x => { throw new TaskCanceledException("Detail page timed out"); }
                );

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().HaveCount(1);

            TrailStatus trail = scraperResult.Trails[0];
            trail.Url.Should().Be(sut.StatusPage);

            using (new AssertionScope())
            {
                ScraperEvent anomaly = scraperResult.Anomalies[0];
                anomaly.Type.Should().Be(ScraperEvent.EventType.Timeout.ToString());
                anomaly.Message.Should().Be("Detail page timed out");
                anomaly.Detail.Should().Be(LinkToValidDetailPage);
            }
        }


        [Fact]
        public async Task Exception_reading_detail_page_creates_anomaly()
        {
            // Arange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                x => Task.FromResult(pageContent),
                x => { throw new Exception("Detail page errored"); }
                );

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().HaveCount(1);

            TrailStatus trail = scraperResult.Trails[0];
            trail.Url.Should().Be(sut.StatusPage);

            using (new AssertionScope())
            {
                ScraperEvent anomaly = scraperResult.Anomalies[0];
                anomaly.Type.Should().Be(ScraperEvent.EventType.Exception.ToString());
                anomaly.Message.Should().Be("Detail page errored");
                anomaly.Detail.Should().Be(LinkToValidDetailPage);
            }
        }


        //
        // Live snapshot tests - prove validity of test data by showing expected behaviour with real data
        //


        [Fact]
        public async Task Live_snapshot_network_open_is_scraped_successfully()
        {
            // Arrange
            const string TestFile = @"Test Data\LiveSnapshotOpen.htm";
            string pageContent = File.ReadAllText(TestFile);

            StatusScraper sut = CreateStatusScraper(TestFile);

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(DetailPageWithValidEnglishLink));
            mockHttpClient.GetStringAsync(Arg.Is<string>(p => p == TestFile)).Returns(
                Task.FromResult(pageContent));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
                scraperResult.Result.Message.Should().Be("73 additional page lookups");
                scraperResult.Result.Detail.Should().Be("8 anomalies found");
            }
            scraperResult.Trails.Should().HaveCount(80);
            scraperResult.Anomalies.Should().HaveCount(8);
        }


        [Fact]
        public async Task Live_snapsot_network_closed_scrapes_successfully()
        {
            // Arrange
            const string TestFile = @"Test Data\LiveSnapshotClosed.htm";
            string pageContent = File.ReadAllText(TestFile);

            StatusScraper sut = CreateStatusScraper(TestFile);

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(DetailPageWithValidEnglishLink));
            mockHttpClient.GetStringAsync(Arg.Is<string>(p => p == TestFile)).Returns(
                Task.FromResult(pageContent));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.DataError.ToString());
                scraperResult.Result.Message.Should().Be("Trail network probably closed");
                scraperResult.Result.Detail.Should().Be("Missing table with id tablepress-14");
            }
            scraperResult.Trails.Should().BeEmpty();
            scraperResult.Anomalies.Should().BeEmpty();
        }


        [Fact]
        public async Task Live_snapshot_detail_page_scraped_successfully()
        {
            // Arrange
            const string TestFile = @"Test Data\LiveSnapshotDetail.htm";
            string detailPageContent = File.ReadAllText(TestFile);
            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            StatusScraper sut = CreateStatusScraper(TestFile);

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(detailPageContent));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
                scraperResult.Result.Message.Should().Be("1 additional page lookups");
                scraperResult.Result.Detail.Should().Be("0 anomalies found");
            }
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().BeEmpty();

            using (new AssertionScope())
            {
                TrailStatus trail = scraperResult.Trails[0];
                trail.Name.Should().Be("GR 130 Etapa 1");
                trail.Status.Should().Be("Open");
                trail.Url.Should().Be(@"https://www.senderosdelapalma.es/en/footpaths/list-of-footpaths/long-distance-footpaths/gr-130-stage-1/");
            }
        }


        // Caching tests

        [Fact]
        public async Task Invalid_cache_not_used()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            sut.UseCache = true;

            var outOfDateScraperResult = new ScraperResult();
            outOfDateScraperResult.Exception("Exception message", "Exception detail");
            CachedResult.Instance.Value = outOfDateScraperResult;

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
                scraperResult.Result.Message.Should().Be("1 additional page lookups");
                scraperResult.Result.Detail.Should().Be("0 anomalies found");
            }
            scraperResult.Trails.Should().HaveCount(1);
            scraperResult.Anomalies.Should().BeEmpty();

            using (new AssertionScope())
            {
                TrailStatus trail = scraperResult.Trails[0];
                trail.Name.Should().Be("GR 130 Etapa 1");
                trail.Status.Should().Be("Open");
                trail.Url.Should().Be("Link_to_English_version.html");
            }
        }


        [Fact]
        public async Task Valid_cashe_is_used()
        {
            // Arrange
            StatusScraper sut = CreateStatusScraper(StatusPageUrl);
            sut.UseCache = true;

            var outOfDateScraperResult = new ScraperResult();
            outOfDateScraperResult.Success("Success message", "Success detail"); // success automatically sets current date-time
            CachedResult.Instance.Value = outOfDateScraperResult;

            string pageContent = SimulateWebPageWithValidTable($@"
                <tr>
                    <td><a href={LinkToValidDetailPage}>GR 130 Etapa 1</a></td>
                    <td>{IgnoredContent}</td>
                    <td>{TrailOpen}</td>
                </tr>
                ");

            var mockHttpClient = Substitute.For<IHttpClient>();
            mockHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(mockHttpClient);

            // Assert
            using (new AssertionScope())
            {
                scraperResult.Result.Type.Should().Be(ScraperEvent.EventType.Success.ToString());
                scraperResult.Result.Message.Should().Be("Success message");
                scraperResult.Result.Detail.Should().Be("Success detail");
            }
            scraperResult.Trails.Should().BeEmpty();
            scraperResult.Anomalies.Should().BeEmpty();
        }
    }
}