using FluentAssertions;
using FluentAssertions.Execution;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Diagnostics.CodeAnalysis;

namespace LaPalmaTrailsAPI.Tests
{
    [ExcludeFromCodeCoverage]
    public class StatusScraperTests
    {

        #region NORMAL OPERATION

        [Fact]
        public async Task Invalid_status_table_creates_data_error()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPage(@"
                <table id=""tablepress-13"">
                </table>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(Task.FromResult(pageContent));

            var expectedResult = new ScraperEvent(
                ScraperEvent.EventType.DataError,
                StatusScraper.ErrorMessage.NetworkClosed,
                StatusScraper.ErrorMessage.NetworkClosedDetail);

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldMatchResult(expectedResult).HaveNoTrails().HaveNoAnomalies();
        }


        [Fact]
        public async Task Valid_trail_scraped_creates_trail_and_success_result()
        {
            // Arrange
            var sut = TestHelper.CreateStatusScraper();

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult
                .ShouldMatchResult(TestHelper.SuccessResult_OneLookup_NoAnomalies)
                .HaveOneTrailThatMatches(TestHelper.Gr130_Open_EnglishLink)
                .HaveNoAnomalies();
        }


        [Fact]
        public async Task Valid_trail_with_extra_zero_after_decimal_point_is_corrected()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPageWithValidTable($@"
                <tr>
                    <td><a href={TestHelper.LinkToValidDetailPage}>PR LP 03.01</a></td>
                    <td>ignored content</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var expectedTrail = new TrailStatus("PR LP 03.1", StatusScraper.Status.Open, TestHelper.LinkToEnglishVersion);

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveNoAnomalies().HaveOneTrailThatMatches(expectedTrail);
        }

        #endregion

        #region TRAIL ERRORS

        [Fact]
        public async Task Invalid_trail_id_results_in_anomaly()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPageWithValidTable($@"
                <tr>
                    <td><a href={TestHelper.LinkToValidDetailPage}>PR 130 Etapa 1</a></td>
                    <td>ignored content</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var expectedAnomaly = new ScraperEvent(
                ScraperEvent.EventType.UnrecognisedTrailId, 
                StatusScraper.ErrorMessage.UnrecognisedId, 
                "PR 130 Etapa 1");

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveNoTrails().HaveOneAnomalyThatMatches(expectedAnomaly);
        }


        [Fact]
        public async Task Missing_trail_URL_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPageWithValidTable($@"
                <tr>
                    <td>PR LP 01</td>
                    <td>ignored content</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var expectedAnomaly = new ScraperEvent(
                ScraperEvent.EventType.BadRouteLink, 
                "PR LP 01", 
                StatusScraper.ErrorMessage.NoLinkToDetail);

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveOneTrail().HaveOneAnomalyThatMatches(expectedAnomaly);
        }


        [Fact]
        public async Task Zip_link_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPageWithValidTable($@"
                <tr>
                    <td><a href=""Dummy.zip"">PR LP 03</a></td>
                    <td>ignored content</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var expectedAnomaly = new ScraperEvent(ScraperEvent.EventType.BadRouteLink, "PR LP 03", "Dummy.zip");

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveOneTrail().HaveOneAnomalyThatMatches(expectedAnomaly);
        }


        [Fact]
        public async Task PDF_link_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPageWithValidTable($@"
                <tr>
                    <td><a href=""Dummy.pdf"">PR LP 02</a></td>
                    <td>ignored content</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var expectedAnomaly = new ScraperEvent(ScraperEvent.EventType.BadRouteLink, "PR LP 02", "Dummy.pdf");

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveOneTrail().HaveOneAnomalyThatMatches(expectedAnomaly);
        }

        #endregion

        #region STATUS TESTS

        [Fact]
        public async Task Part_open_status_links_to_status_page()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPageWithValidTable($@"
                <tr>
                    <td><a href={TestHelper.LinkToValidDetailPage}>PR LP 01</a></td>
                    <td>ignored content</td>
                    <td>Abierto / Open / Geöffnet and additional content</td>
                </tr>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var expectedTrail = new TrailStatus("PR LP 01", StatusScraper.Status.PartOpen, sut.StatusPage);

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveOneTrailThatMatches(expectedTrail).HaveNoAnomalies();
        }


        [Fact]
        public async Task Uncertain_status_creates_anomaly()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPageWithValidTable($@"
                <tr>
                    <td><a href={TestHelper.LinkToValidDetailPage}>PR LP 01</a></td>
                    <td>ignored content</td>
                    <td>Blah blah blah</td>
                </tr>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var expectedTrail = new TrailStatus("PR LP 01", StatusScraper.Status.Unknown, sut.StatusPage);

            var expectedAnomaly = new ScraperEvent(
                ScraperEvent.EventType.UnreadableStatus, 
                "PR LP 01", 
                "Blah blah blah");

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveOneTrailThatMatches(expectedTrail).HaveOneAnomalyThatMatches(expectedAnomaly);
        }

        #endregion

        #region TIMEOUTS AND EXCEPTIONS

        [Fact]
        public async Task Timeout_reading_status_page_creates_timeout_result()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>())
                .Returns(Task.FromException<string>(new TaskCanceledException("Status page timed out")));

            var expectedResult = new ScraperEvent(ScraperEvent.EventType.Timeout, "Status page timed out", sut.StatusPage);

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldMatchResult(expectedResult).HaveNoTrails().HaveNoAnomalies();
        }


        [Fact]
        public async Task Exception_reading_status_page_creates_exception_result()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>())
                .Returns(Task.FromException<string>(new Exception("Random error")));

            var expectedResult = new ScraperEvent(
                ScraperEvent.EventType.Exception, 
                StatusScraper.ErrorMessage.GeneralException, 
                "Random error");

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldMatchResult(expectedResult).HaveNoTrails().HaveNoAnomalies();
        }

        #endregion

        #region DETAIL PAGE LOOKUPS

        [Fact]
        public async Task English_URL_not_in_map_gets_added_and_returned()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper(clearLookups: false);

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            var expectedResult = TestHelper.SuccessResult_OneLookup_NoAnomalies;
            var expectedTrail = TestHelper.Gr130_Open_EnglishLink;

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldMatchResult(expectedResult).HaveOneTrailThatMatches(expectedTrail).HaveNoAnomalies();
        }


        [Fact]
        public async Task English_URL_not_found_creates_anomaly_returns_status_page()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            string pageContent = TestHelper.StatusPageWithValidTable($@"
                <tr>
                    <td><a href=""Detail_page_no_English_link.html"">GR 130 Etapa 1</a></td>
                    <td>ignored content</td>
                    <td>{TestHelper.TrailOpen}</td>
                </tr>
                ");

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(pageContent),
                Task.FromResult(@"<link rel=""alternate"" hreflang=""de"" href=""Link_to_German_version.html"" />"));

            var expectedAnomaly = new ScraperEvent(
                ScraperEvent.EventType.BadRouteLink, 
                StatusScraper.ErrorMessage.EnglishUrlNotFound, 
                "Detail_page_no_English_link.html");

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveOneTrail().HaveOneAnomalyThatMatches(expectedAnomaly);
            scraperResult.Trails[0].Url.Should().Be(sut.StatusPage);
        }


        [Fact]
        public async Task Timeout_reading_detail_page_creates_anomaly_returns_status_page()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper();

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                x => Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                x => { throw new TaskCanceledException("Detail page timed out"); }
                );

            var expectedAnomaly = new ScraperEvent(
                ScraperEvent.EventType.Timeout, 
                "Detail page timed out", 
                TestHelper.LinkToValidDetailPage);

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveOneTrail().HaveOneAnomalyThatMatches(expectedAnomaly);
            scraperResult.Trails[0].Url.Should().Be(sut.StatusPage);
        }


        [Fact]
        public async Task Exception_reading_detail_page_creates_anomaly()
        {
            // Arange
            StatusScraper sut = TestHelper.CreateStatusScraper(TestHelper.StatusPageUrl);

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                x => Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                x => { throw new Exception("Detail page errored"); }
                );

            var expectedAnomaly = new ScraperEvent(
                ScraperEvent.EventType.Exception, 
                "Detail page errored", 
                TestHelper.LinkToValidDetailPage);

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldBeSuccess().HaveOneTrail().HaveOneAnomalyThatMatches(expectedAnomaly);
            scraperResult.Trails[0].Url.Should().Be(sut.StatusPage);
        }

        #endregion

        #region LIVE SNAPSHOT TESTS

        [Fact]
        public async Task Live_snapshot_network_open_is_scraped_successfully()
        {
            // Arrange
            const string TestFile = @"Test Data\LiveSnapshotOpen.htm";
            string pageContent = File.ReadAllText(TestFile);

            StatusScraper sut = TestHelper.CreateStatusScraper(testPage: TestFile);

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));
            stubHttpClient.GetStringAsync(Arg.Is<string>(p => p == TestFile)).Returns(
                Task.FromResult(pageContent));

            var expectedResult = new ScraperEvent(
                ScraperEvent.EventType.Success, 
                "73 additional page lookups", 
                "8 anomalies found");

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.Result.ShouldMatch(expectedResult);
            scraperResult.Trails.Should().HaveCount(80);
            scraperResult.Anomalies.Should().HaveCount(8);
        }


        [Fact]
        public async Task Live_snapsot_network_closed_scrapes_successfully()
        {
            // Arrange
            const string TestFile = @"Test Data\LiveSnapshotClosed.htm";
            string pageContent = File.ReadAllText(@"Test Data\LiveSnapshotClosed.htm");

            StatusScraper sut = TestHelper.CreateStatusScraper(testPage: TestFile);

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));
            stubHttpClient.GetStringAsync(Arg.Is<string>(p => p == TestFile)).Returns(
                Task.FromResult(pageContent));

            var expectedResult = new ScraperEvent(
                ScraperEvent.EventType.DataError, 
                StatusScraper.ErrorMessage.NetworkClosed, 
                StatusScraper.ErrorMessage.NetworkClosedDetail);

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldMatchResult(expectedResult).HaveNoTrails().HaveNoAnomalies();
        }


        [Fact]
        public async Task Live_snapshot_detail_page_scraped_successfully()
        {
            // Arrange
            string detailPageContent = File.ReadAllText(@"Test Data\LiveSnapshotDetail.htm");

            StatusScraper sut = TestHelper.CreateStatusScraper();

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                Task.FromResult(detailPageContent));

            var expectedResult = TestHelper.SuccessResult_OneLookup_NoAnomalies;

            var expectedTrail = new TrailStatus(
                "GR 130 Etapa 1", 
                StatusScraper.Status.Open, 
                @"https://www.senderosdelapalma.es/en/footpaths/list-of-footpaths/long-distance-footpaths/gr-130-stage-1/");

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.ShouldMatchResult(expectedResult).HaveOneTrailThatMatches(expectedTrail).HaveNoAnomalies();
        }


        #endregion

        #region CACHING TESTS

        [Fact]
        public async Task Invalid_cache_not_used()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper(useCache: true);

            var invalidScraperResult = new ScraperResult();
            invalidScraperResult.Exception("Exception message", "Exception detail"); // sets cache to invalid result
            CachedResult.Instance.Value = invalidScraperResult;

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult
                .ShouldMatchResult(TestHelper.SuccessResult_OneLookup_NoAnomalies)
                .HaveOneTrailThatMatches(TestHelper.Gr130_Open_EnglishLink)
                .HaveNoAnomalies();
        }


        [Fact]
        public async Task Valid_cashe_is_used()
        {
            // Arrange
            StatusScraper sut = TestHelper.CreateStatusScraper(useCache: true);

            var oldScraperResult = new ScraperResult();
            oldScraperResult.Success("Success message", "Success detail"); // success automatically sets current date-time
            CachedResult.Instance.Value = oldScraperResult;

            var stubHttpClient = Substitute.For<IHttpClient>();
            stubHttpClient.GetStringAsync(Arg.Any<string>()).Returns(
                Task.FromResult(TestHelper.StatusPageWithWithSingleOpenGr130ValidDetailLink),
                Task.FromResult(TestHelper.DetailPageWithValidEnglishLink));

            // Act
            var scraperResult = await sut.GetTrailStatuses(stubHttpClient);

            // Assert
            scraperResult.Should().BeSameAs(oldScraperResult);
        }

        #endregion
    }
}