namespace lpfwAPI
{
    // Records scraping errors
    public class ScraperEvent
    {
        public enum EventType 
        { 
            // error values
            Success,
            Exception, 
            Timeout, 
            DataError,

            // anomaly values
            BadRouteLink, 
            UnrecognisedTrailId, 
            UnreadableStatus,
            Unexpected
        };

        public string Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;

        public ScraperEvent()
        {
            Type = EventType.Success.ToString();
            Message = "";
            Detail = "";
        }


        public ScraperEvent(EventType type, string message, string detail)
        {
            Type = type.ToString();
            Message = message;
            Detail = detail;
        }
    }
}
