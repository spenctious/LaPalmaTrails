namespace lpfwAPI
{
    // Records trail status information
    public class TrailStatus
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public TrailStatus(string trail, string status, string url)
        {
            Name = trail;
            Status = status;
            Url = url;
        }
    }
}
