namespace LaPalmaTrailsAPI
{
    /// <summary>
    /// Lazy thread-safe singleton implementation to cache a ScraperResult
    /// </summary>
    public sealed class CachedResult
    {
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

        private CachedResult()
        {
        }
    }
}
