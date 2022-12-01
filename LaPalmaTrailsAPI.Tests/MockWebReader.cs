using LaPalmaTrailsAPI;

namespace LaPalmaTrailsAPI.Tests
{
    public class MockWebReader : IWebReader
    {
        TimeSpan _timeout = TimeSpan.FromMilliseconds(1000);

        public async Task<string> GetStringAsync(string? testPage)
        {
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

