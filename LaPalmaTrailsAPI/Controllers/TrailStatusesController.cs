using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace lpfwAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScraperResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class TrailStatusesController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(
            string? statusPage = null,
            int? statusPageTimeout = null,
            int? detailPageTimeout = null,
            bool? useCache = null,
            bool? clearLookups = null)
            {
            try
                {
                var statusScraper = new StatusScraper();
                // override defaults with parameter values if set
                if (statusPage != null) { statusScraper.StatusPage = statusPage; }
                if (statusPageTimeout != null) { statusScraper.StatusPageTimeout = (int)statusPageTimeout; }
                if (detailPageTimeout != null) { statusScraper.DetailPageTimeout = (int)detailPageTimeout; }
                if (useCache != null) { statusScraper.UseCache = (bool)useCache; }
                if (clearLookups != null) { statusScraper.ClearLookups = (bool)clearLookups; }

                var scraperResult = await statusScraper.GetTrailStatuses();
                return Ok(scraperResult);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
