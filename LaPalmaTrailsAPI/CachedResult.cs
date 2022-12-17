namespace LaPalmaTrailsAPI
{
    /// <summary>
    /// Lazy thread-safe singleton implementation to cache a ScraperResult
    /// </summary>
    public sealed class CachedResult
    {
        const int DaysBeforeOutOfDate = 1;
        private ScraperResult _result = new();
        private readonly object _padlock = new();

        public ScraperResult Value
        {
            get
            {
                lock (_padlock)
                {
                    return _result;
                }
            }

            set
            {
                lock (_padlock)
                {
                    _result = value;
                }
            }
        }

        private static readonly Lazy<CachedResult> lazy = new Lazy<CachedResult>(() => new());

        public static CachedResult Instance { get { return lazy.Value; } }

        public static bool IsValid
        {
            get
            {
                // should not be caching anything other than successful results
                if (!CachedResult.Instance.Value.IsSuccess) return false;

                TimeSpan span = DateTime.Now.Subtract(CachedResult.Instance.Value.LastScraped);
                return (int)span.TotalDays < DaysBeforeOutOfDate;
            }
        }

        private CachedResult()
        {
        }
    }
}
