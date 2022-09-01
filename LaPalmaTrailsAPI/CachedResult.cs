namespace LaPalmaTrailsAPI
{
    /// <summary>
    /// Lazy thread-safe singleton implementation to cache a ScraperResult
    /// </summary>
    public sealed class CachedResult
    {
        public ScraperResult Value { get; set; } = new();

        private static readonly Lazy<CachedResult> lazy = new Lazy<CachedResult>(() => new());

        public static CachedResult Instance { get { return lazy.Value; } }

        private CachedResult()
        {
        }
    }
}
