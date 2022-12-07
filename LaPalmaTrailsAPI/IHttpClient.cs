namespace LaPalmaTrailsAPI
{
    public interface IHttpClient
    {
        public Task<string> GetStringAsync(string? requestUri);
        public TimeSpan Timeout { get; set; }
    }
}
