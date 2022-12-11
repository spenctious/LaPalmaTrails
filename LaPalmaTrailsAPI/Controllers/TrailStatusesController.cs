using Microsoft.AspNetCore.Mvc;

namespace LaPalmaTrailsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScraperResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class TrailStatusesController : ControllerBase
    {
        private readonly IHttpClient _httpClient;
        public TrailStatusesController(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            // optional parameters - mainly used for testing
            string? statusPage = null,      // override default page to scrape (test only)
            int? statusPageTimeout = null,  // override the default timeout (mainly for testing)
            int? detailPageTimeout = null,  // override the default timeout (mainly for testing)
            bool? useCache = null,          // set to true to ignore the cache and scrape the page fresh (mainly test)
            bool? clearLookups = null)      // set to true to force force the lookup table to be rebuilt (test only)
        {
            try
            {
                StatusScraper statusScraper = new();

                // override defaults with parameter values if set
                if (statusPage != null)         { statusScraper.StatusPage          = statusPage; }
                if (statusPageTimeout != null)  { statusScraper.StatusPageTimeout   = (int)statusPageTimeout; }
                if (detailPageTimeout != null)  { statusScraper.DetailPageTimeout   = (int)detailPageTimeout; }
                if (useCache != null)           { statusScraper.UseCache            = (bool)useCache; }
                if (clearLookups != null)       { statusScraper.ClearLookups        = (bool)clearLookups; }

                var scraperResult = await statusScraper.GetTrailStatuses(_httpClient);
                return Ok(scraperResult);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
