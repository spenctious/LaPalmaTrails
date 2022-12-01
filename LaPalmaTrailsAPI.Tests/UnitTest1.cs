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


        private bool TrailFound(List<TrailStatus> trailList, string status, string url, string name)
        {
            return trailList.Find(t => 
                t.Status == status && 
                t.Url == url && 
                t.Name == name) != null;
        }


        private bool UnrecognisedTrailAnomaly(List<ScraperEvent> eventList, string detail)
        {
            return eventList.Find(t => 
                t.Type == ScraperEvent.EventType.UnrecognisedTrailId.ToString() && 
                t.Message == "Unrecognised trail" && 
                t.Detail == detail) != null;
        }


        [Fact]
        public async Task Valid_GR_paths_recognised()
        {
            StatusScraper sut = CreateStatusScraper("Valid_GR_trails.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal(2, scraperResult.Trails.Count);
            Assert.Empty(scraperResult.Anomalies);

            Assert.True(TrailFound(scraperResult.Trails, "Open", "Link_to_English_version.html", "GR 130 Etapa 1"));
            Assert.True(TrailFound(scraperResult.Trails, "Open", "Link_to_English_version.html", "GR 131 Etapa 1"));
        }


        [Fact]
        public async Task Valid_PR_paths_recognised()
        {
            StatusScraper sut = CreateStatusScraper("Valid_PR_trails.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal(3, scraperResult.Trails.Count);
            Assert.Empty(scraperResult.Anomalies);

            Assert.True(TrailFound(scraperResult.Trails, "Open", "Link_to_English_version.html", "PR LP 01"));
            Assert.True(TrailFound(scraperResult.Trails, "Open", "Link_to_English_version.html", "PR LP 200"));
            Assert.True(TrailFound(scraperResult.Trails, "Open", "Link_to_English_version.html", "PR LP 02.1"));
        }


        [Fact]
        public async Task Valid_PR_trail_with_extra_digit_is_corrected()
        {
            StatusScraper sut = CreateStatusScraper("Valid_PR_trail_extra_digit.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);

            Assert.True(TrailFound(scraperResult.Trails, "Open", "Link_to_English_version.html", "PR LP 03.1"));
        }


        [Fact]
        public async Task Valid_SL_paths_recognised()
        {
            StatusScraper sut = CreateStatusScraper("Valid_SL_trails.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal(2, scraperResult.Trails.Count);
            Assert.Empty(scraperResult.Anomalies);

            Assert.True(TrailFound(scraperResult.Trails, "Open", "Link_to_English_version.html", "SL BV 01"));
            Assert.True(TrailFound(scraperResult.Trails, "Open", "Link_to_English_version.html", "SL BV 200"));
        }

        [Fact]
        public async Task Invalid_paths_recorded_as_anomalies()
        {
            StatusScraper sut = CreateStatusScraper("Invalid_trails.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal(5, scraperResult.Anomalies.Count);
            Assert.Empty(scraperResult.Trails);

            Assert.True(UnrecognisedTrailAnomaly(scraperResult.Anomalies, "PR 130 Etapa 1"));
            Assert.True(UnrecognisedTrailAnomaly(scraperResult.Anomalies, "GR 120 Etapa 1"));
            Assert.True(UnrecognisedTrailAnomaly(scraperResult.Anomalies, "GR 130.1"));
            Assert.True(UnrecognisedTrailAnomaly(scraperResult.Anomalies, "PL LP 12"));
            Assert.True(UnrecognisedTrailAnomaly(scraperResult.Anomalies, "PR LP 1"));
        }


    }
}