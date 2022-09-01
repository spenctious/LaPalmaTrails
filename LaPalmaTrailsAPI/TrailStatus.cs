namespace LaPalmaTrailsAPI
{
    /// <summary>
    /// Records trail status information and the most appropriate link for trail detail in English
    /// </summary>
    public class TrailStatus
    {
        public string Name { get; } = string.Empty;
        public string Status { get; } = string.Empty;
        public string Url { get; } = string.Empty;

        public TrailStatus(string trail, string status, string url)
        {
            Name = trail;
            Status = status;
            Url = url;
        }
    }
}
