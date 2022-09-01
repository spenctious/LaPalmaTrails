using System.Collections.Concurrent;

namespace LaPalmaTrailsAPI
{
    public class ScraperResult
    {
        private ScraperEvent result = new();
        private readonly ConcurrentBag<TrailStatus> statuses = new();
        private readonly ConcurrentBag<ScraperEvent> anomalies = new();

        public DateTime LastScraped { get; set; }
        public ScraperEvent Result { get { return result; } }
        public List<TrailStatus> Trails { get { return statuses.ToList(); } }
        public List<ScraperEvent> Anomalies { get { return anomalies.ToList(); } }


        public ScraperResult() 
        { 
            LastScraped = DateTime.MinValue; 
        }

        // ************* Successful result

        public void Success(string message, string detail)
        {
            result = new ScraperEvent(ScraperEvent.EventType.Success, message, detail);
            LastScraped = DateTime.Now;
        }

        // ************* Failure results

        public void Exception(string message, string detail)
        {
            result = new ScraperEvent(ScraperEvent.EventType.Exception, message, detail);
        }

        public void Timeout(string message, string detail)
        {
            result = new ScraperEvent(ScraperEvent.EventType.Timeout, message, detail);
        }

        public void DataError(string message, string detail)
        {
            result = new ScraperEvent(ScraperEvent.EventType.DataError, message, detail);
        }

        // ************* Successful scraping events and anomalies

        public void AddAnomaly(ScraperEvent.EventType type, string name, string details)
        {
            anomalies.Add(new ScraperEvent(type, name, details));
        }

        public void AddTrailStatus(string trailName, string trailStatus, string url)
        {
            statuses.Add(new TrailStatus(trailName, trailStatus, url));
        }
    }
}
