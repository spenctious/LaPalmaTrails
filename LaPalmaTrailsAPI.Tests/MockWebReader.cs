using LaPalmaTrailsAPI;

namespace LaPalmaTrailsAPI.Tests
{
    public class MockWebReader : IWebReader
    {
        TimeSpan _timeout = TimeSpan.FromMilliseconds(1000);
        public Exception? SimulatedException { get; set; } = null;

        public async Task<string> GetStringAsync(string? testPage)
        {
            // simulate a timeout
            if (SimulatedException != null) throw SimulatedException;

            string testDataSource = @"C:\Users\Robert\projects\LaPalmaTrailsAPI\LaPalmaTrailsAPI.Tests\TestData\";
            return await File.ReadAllTextAsync(testDataSource + (testPage ?? ""));
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

