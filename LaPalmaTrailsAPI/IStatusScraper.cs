namespace LaPalmaTrailsAPI
{
    public interface IStatusScraper
    {
        bool ClearLookups { get; set; }
        int DetailPageTimeout { get; set; }
        string StatusPage { get; set; }
        int StatusPageTimeout { get; set; }
        bool UseCache { get; set; }

        Task<string> GetEnglishUrl(IHttpClient webReader, string routeId, string spanishUrl, ScraperResult scraperResult);
        Task<ScraperResult> GetTrailStatuses(IHttpClient webReader);
    }
}