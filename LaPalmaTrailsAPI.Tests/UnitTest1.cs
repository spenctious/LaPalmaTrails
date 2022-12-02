using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace LaPalmaTrailsAPI.Tests
{
    public class StatusScraperTests
    {

        private StatusScraper CreateStatusScraper(string testPage)
        {
            StatusScraper scraper = new();
            scraper.StatusPage = testPage;
            scraper.ClearLookups = true;
            scraper.UseCache = false;

            return scraper;
        }


        [Fact]
        public async Task Invalid_status_table_creates_data_error()
        {
            StatusScraper sut = CreateStatusScraper("Invalid_Table.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

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
            StatusScraper sut = CreateStatusScraper("Valid_trail.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal("1 additional page lookups", scraperResult.Result.Message);
            Assert.Equal("0 anomalies found", scraperResult.Result.Detail);

            Assert.Single(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);

            TrailStatus ts = scraperResult.Trails[0];
            Assert.Equal("GR 130 Etapa 1", ts.Name);
            Assert.Equal("Open", ts.Status);
            Assert.Equal("Link_to_English_version.html", ts.Url);
        }


        [Fact]
        public async Task Invalid_trail_id_results_in_anomaly()
        {
            StatusScraper sut = CreateStatusScraper("Invalid_trail.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

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
            StatusScraper sut = CreateStatusScraper("Valid_trail_extra_digit.html"); // PR LP 03.01

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

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
            StatusScraper sut = CreateStatusScraper("Missing_detail_link.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

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
            StatusScraper sut = CreateStatusScraper("Invalid_Zip_link.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

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
            StatusScraper sut = CreateStatusScraper("Invalid_PDF_link.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

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
            StatusScraper sut = CreateStatusScraper("Valid_partly_open.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

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
            StatusScraper sut = CreateStatusScraper("Valid_open_status_unknown.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Single(scraperResult.Anomalies);

            ScraperEvent anomaly = scraperResult.Anomalies[0];
            Assert.Equal(ScraperEvent.EventType.UnreadableStatus.ToString(), anomaly.Type);
            Assert.Equal("PR LP 01", anomaly.Message);
            Assert.Equal("Blah blah blah", anomaly.Detail);
        }


        [Fact]
        public async Task Additinal_link_updates_lookup_table_on_file()
        {
            Assert.False(true);
        }


        [Fact]
        public async Task Timeout_reading_status_page_creates_timeout_result()
        {
            StatusScraper sut = CreateStatusScraper("Valid_trail.html");
            MockWebReader mockWebReader = new();
            mockWebReader.SimulatedException = new TaskCanceledException("MockWebReader timed out");

            var scraperResult = await sut.GetTrailStatuses(mockWebReader);

            Assert.Equal(ScraperEvent.EventType.Timeout.ToString(), scraperResult.Result.Type);
            Assert.Equal("MockWebReader timed out", scraperResult.Result.Message);
            Assert.Equal(sut.StatusPage, scraperResult.Result.Detail);

            Assert.Empty(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);
        }


        [Fact]
        public async Task Exception_reading_status_page_creates_exception_result()
        {
            Assert.False(true);
        }


        // links

        [Fact]
        public async Task English_URL_not_in_map_gets_added()
        {
            Assert.False(true);
        }


        [Fact]
        public async Task English_URL_in_map_gets_returned()
        {
            Assert.False(true);
        }


        [Fact]
        public async Task English_URL_not_found_creates_anomaly()
        {
            Assert.False(true);
        }


        [Fact]
        public async Task Timeout_reading_detail_page_creates_anomaly()
        {
            Assert.False(true);
        }


        [Fact]
        public async Task Exception_reading_detail_page_creates_anomaly()
        {
            Assert.False(true);
        }
    }
}