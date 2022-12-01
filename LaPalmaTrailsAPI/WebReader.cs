namespace LaPalmaTrailsAPI
{
    public class WebReader : IWebReader
    {
        HttpClient httpClient;

        public WebReader()
        {
            httpClient = new();
        }

        public Task<string> GetStringAsync(string? requestUri)
        {
            return httpClient.GetStringAsync(requestUri);
        }

        public TimeSpan Timeout {
            get
            {
                return httpClient.Timeout;
            }

            set
            {
                httpClient.Timeout = value;
            }
        }
    }
}
