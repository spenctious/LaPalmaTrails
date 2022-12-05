using LaPalmaTrailsAPI;

namespace LaPalmaTrailsAPI.Tests
{
    public class MockWebReader : IWebReader
    {
        TimeSpan _timeout = TimeSpan.FromMilliseconds(1000);
        private string _simulatedContent = "";

        public Dictionary<string, Exception> SimulateException = new();
        public Dictionary<string, string> SimulatedWebPage = new();

        public MockWebReader()
        {
            // content for default detail page
            SimulatedWebPage.Add(
                "Dummy_trail_detail_page.html", 
                SimulateWebPage(@"<link rel=""alternate"" hreflang=""en-us"" href=""Link_to_English_version.html"" />"));
        }

        public static string SimulateWebPage(string bodyContent)
        {
            return $@"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                  <meta charset=""UTF-8"">
                  <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
                  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                  <title>Test 1</title>
                </head>
                <body>
                    {bodyContent}
                </body>
                </html>";
        }

        public static string SimulateWebPageWithTableId14(string tableContent)
        {
            return SimulateWebPage($@"
                <table id=""tablepress-14"">
                  <thead>
                    <tr>
                      <th>Route</th>
                      <th>Other stuff</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tableContent}
                  </tbody>
                </table>");
        }

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

