namespace LaPalmaTrailsAPI.Tests
{
    public class StatusScraperTests
    {
        [Fact]
        public async Task Valid_GR_paths_recognised()
        {
            StatusScraper sut = new();
            sut.StatusPage = "Valid_GR_trails.html";

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
    }
}