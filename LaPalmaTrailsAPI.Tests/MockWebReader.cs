using LaPalmaTrailsAPI;

namespace LaPalmaTrailsAPI.Tests
{
    public class MockWebReader : IHttpClient
    {
        TimeSpan _timeout = TimeSpan.FromMilliseconds(1000);

        // allow the calling test to define behaviour by specifying content to be returned
        // or exception thrown, depending on the page specified
        public Dictionary<string, Exception> SimulateException = new();
        public Dictionary<string, string> SimulatedWebPage = new();


        /// <summary>
        /// Either returns the simulated web content associated with the test page or throws
        /// an exception.
        /// </summary>
        /// <param name="testPage"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> GetStringAsync(string? testPage)
        {
            Exception? exception;
            if (SimulateException.TryGetValue(testPage ?? "", out exception))
            {
                throw exception;
            }

            string? mockWebContent;
            if (!SimulatedWebPage.TryGetValue(testPage ?? "", out mockWebContent))
            {
                throw new Exception("Test page not found");
            }

            await Task.Delay(0);
            return mockWebContent;
        }

        public TimeSpan Timeout
        {
            get
            {
                return _timeout;
            }

            set
            {
                _timeout = value;
            }
        }
    }
}

