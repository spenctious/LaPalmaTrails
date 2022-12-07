namespace LaPalmaTrailsAPI
{
    public class HttpClientWrapper : IHttpClient
    {
        HttpClient httpClient;

        public HttpClientWrapper()
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
