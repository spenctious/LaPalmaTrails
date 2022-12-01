namespace LaPalmaTrailsAPI
{
    public interface IWebReader
    {
        public Task<string> GetStringAsync(string? requestUri);
        public TimeSpan Timeout { get; set; }
    }
}
