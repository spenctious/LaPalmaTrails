namespace LaPalmaTrailsAPI
{
    /// <summary>
    /// Records scraping events: successful outcomes, errors and anomalies
    /// </summary>
    public class ScraperEvent
    {
        public enum EventType
        {
            // ok
            Success,

            // errors
            Exception,
            Timeout,
            DataError,

            // anomalies
            BadRouteLink,
            UnrecognisedTrailId,
            UnreadableStatus
        };

        public string Type { get; }
        public string Message { get; } = string.Empty;
        public string Detail { get; } = string.Empty;

        // Default event is success
        public ScraperEvent()
        {
            Type = EventType.Success.ToString();
        }


        public ScraperEvent(EventType type, string message, string detail)
        {
            Type = type.ToString();
            Message = message;
            Detail = detail;
        }
    }
}
