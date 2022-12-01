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
        public async Task Valid_GR_paths_recognised()
        {
            StatusScraper sut = CreateStatusScraper("Valid_GR_trails.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal(2, scraperResult.Trails.Count);
            Assert.Empty(scraperResult.Anomalies);

            // N.B. scraper results are listed in reverse order to their appearance in the test file

            Assert.Equal("Open",                            scraperResult.Trails[0].Status);
            Assert.Equal("Link_to_English_version.html",    scraperResult.Trails[0].Url);
            Assert.Equal("GR 131 Etapa 1",                  scraperResult.Trails[0].Name);

            Assert.Equal("Open",                            scraperResult.Trails[1].Status);
            Assert.Equal("Link_to_English_version.html",    scraperResult.Trails[1].Url);
            Assert.Equal("GR 130 Etapa 1",                  scraperResult.Trails[1].Name);
        }


        [Fact]
        public async Task Valid_PR_paths_recognised()
        {
            StatusScraper sut = CreateStatusScraper("Valid_PR_trails.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal(3, scraperResult.Trails.Count);
            Assert.Empty(scraperResult.Anomalies);

            // N.B. scraper results are listed in reverse order to their appearance in the test file

            Assert.Equal("Open",                            scraperResult.Trails[0].Status);
            Assert.Equal("Link_to_English_version.html",    scraperResult.Trails[0].Url);
            Assert.Equal("PR LP 02.1",                      scraperResult.Trails[0].Name);

            Assert.Equal("Open",                            scraperResult.Trails[1].Status);
            Assert.Equal("Link_to_English_version.html",    scraperResult.Trails[1].Url);
            Assert.Equal("PR LP 200",                       scraperResult.Trails[1].Name);

            Assert.Equal("Open",                            scraperResult.Trails[2].Status);
            Assert.Equal("Link_to_English_version.html",    scraperResult.Trails[2].Url);
            Assert.Equal("PR LP 01",                        scraperResult.Trails[2].Name);
        }


        [Fact]
        public async Task Valid_PR_trail_with_extra_digit_is_corrected()
        {
            StatusScraper sut = CreateStatusScraper("Valid_PR_trail_extra_digit.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Single(scraperResult.Trails);
            Assert.Empty(scraperResult.Anomalies);

            Assert.Equal("Open",                            scraperResult.Trails[0].Status);
            Assert.Equal("Link_to_English_version.html",    scraperResult.Trails[0].Url);
            Assert.Equal("PR LP 03.1",                      scraperResult.Trails[0].Name);
        }


        [Fact]
        public async Task Valid_SL_paths_recognised()
        {
            StatusScraper sut = CreateStatusScraper("Valid_SL_trails.html");

            var scraperResult = await sut.GetTrailStatuses(new MockWebReader());

            Assert.Equal(ScraperEvent.EventType.Success.ToString(), scraperResult.Result.Type);
            Assert.Equal(2, scraperResult.Trails.Count);
            Assert.Empty(scraperResult.Anomalies);

            // N.B. scraper results are listed in reverse order to their appearance in the test file

            Assert.Equal("Open", scraperResult.Trails[0].Status);
            Assert.Equal("Link_to_English_version.html", scraperResult.Trails[0].Url);
            Assert.Equal("SL BV 200", scraperResult.Trails[0].Name);

            Assert.Equal("Open", scraperResult.Trails[1].Status);
            Assert.Equal("Link_to_English_version.html", scraperResult.Trails[1].Url);
            Assert.Equal("SL BV 01", scraperResult.Trails[1].Name);
        }

    }
}