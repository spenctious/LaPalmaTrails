using System.Collections.Concurrent;
using static lpfwAPI.ScraperEvent;

namespace lpfwAPI
{
    public class ScraperResult
    {
        private readonly ScraperEvent result = new();
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

        public void RecordFatalError(ScraperEvent.EventType type, string message, string detail)
        {
            lock(Result)
            {
                result.Type = type.ToString();
                result.Message = message;
                result.Detail = detail;
            }
        }

        public void AddAnomaly(EventType type, string message, string detail)
        {
            anomalies.Add(new ScraperEvent(type, message, detail));
        }

        public void AddTrailStatus(string trail, string status, string url)
        {
            statuses.Add(new TrailStatus(trail, status, url));
        }
    }
}
