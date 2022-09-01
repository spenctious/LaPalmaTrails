# LaPalmaTrailsAPI

An API for trail status information for the Canary island of La Palma

## Usage

The API exposes only one GET method at endpoint `/api/TrailStatuses`

## Optional parameters

Optional parameters are provided mainly for testing:
| Parameter | Default | Useage |
|-----------|---------|--------|
| statusPage | https://www.senderosdelapalma.es/en/footpaths/situation-of-the-footpaths/ | testing only |
| statusPageTimeout | 6s | mainly testing |
| detailPageTimeout | 6s | mainly testing |
| useCache | true | mainly testing - can be used to force a refresh if needed |
| clearLookups | false | testing only - clears the Spanish to English route detail lookup table |

## Output

The API returns HTTP response 200 with JSON in the following format (Swagger example):
```
{
  "lastScraped": "2022-08-31T13:45:49.556Z",
  "result": {
    "type": "string",
    "message": "string",
    "detail": "string"
  },
  "trails": [
    {
      "name": "string",
      "status": "string",
      "url": "string"
    }
  ],
  "anomalies": [
    {
      "type": "string",
      "message": "string",
      "detail": "string"
    }
  ]
}
```
or HTTP response 500

#### Result types:
- ***Success***
- ***Exception***
- ***Timeout***
- ***DataError*** (usually means the trail network as a whole is closed)

#### Trails:
- ***Name*** is the trail identifier (e.g. PR LP 01) and corrected for known website anomalies or 'Unrecognised trail' if the identifier did not match known formats. Clients are responsible for performing translations ('Etapa' to 'Stage' for example) and simplify numbering to match the route signs on the trails ('PR LP 01' to 'PR LP 1' for example).
- ***Status*** is either Open, Part open, Closed or 'Unknown' if the status can't be properly determined.
- ***Url*** is the English translation of the route detail page if it exists and can be determined, or the status page as a fallback.

### Anomalies:
Anomaly events are anything non-fatal the scraper encounters. While mainly diagnostic in nature, a consuming app can use the results if desired. For example, some table entries link to a PDF or ZIP file of a GPX route if a proper route detail page is not available. These links are not returned in the trails list but can be retrieved from the anomalies list if so desired.

## Caching

The API caches results to reduce the load on the scraped server. Results are considered fresh if they are less than a day old. This is usually sufficient but can be overriden by using the optional parameter.

Although the scraped site is multi-lingual the status page only links to the Spanish version of route detail pages. To get the English version these Spanish pages have to be scraped themselves. Retrieved links are cached so we only need to do this once. An initial call to scrape the status page is performed at start-up which builds the table (taking a couple of minutes or so to complete). Clearing the cache will cause the next call to try to rebuild it and will likely timeout unless it is given a lot of latitude.

## Technical notes
Attempting to speed up the building of the link cache by scraping the links in parallel does not work: the calls seem to be queued by the server.
Caches are shared resources so need to be explicitly made thread-safe.
