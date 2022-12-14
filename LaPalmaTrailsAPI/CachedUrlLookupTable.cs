using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace LaPalmaTrailsAPI
{
    /// <summary>
    /// A standard concurrent dictionary as a singleton with file persistance
    /// </summary>
    public sealed class CachedUrlLookupTable
    {
        private ConcurrentDictionary<string, string> _urlMap = new();
        private readonly object _padlock = new();

        public const string UrlTableLookupFileName = "urlLookupTable.txt";

        public ConcurrentDictionary<string, string> Value
        {
            get
            {
                lock (_padlock)
                {
                    return _urlMap;
                }
            }

            set
            {
                lock (_padlock)
                {
                    _urlMap = value;
                }
            }
        }


        public bool LoadFromFile()
        {
            lock (_padlock)
            {
                // no file to load
                if (!File.Exists(UrlTableLookupFileName)) return false;

                // deserialize from JSON file and check the read worked
                try
                {
                    var dictionary = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>
                        (File.ReadAllText(UrlTableLookupFileName));

                    // validate some data was returned
                    if (dictionary == null || dictionary.Count == 0) return false;

                    // repopulate the map
                    _urlMap = new ConcurrentDictionary<string, string>(dictionary);
                }
                catch (Exception ex)
                {
                    return false;
                }
                return true;
            }
        }


        /// <summary>
        /// Save the URL lookup map to file.
        /// Call whenever additional lookups have had to be made
        /// </summary>
        public void SaveToFile()
        {
            lock (_padlock)
            {
                // overwrites file if it already exists or creates it new if it doesn't
                File.WriteAllText(UrlTableLookupFileName, JsonConvert.SerializeObject(_urlMap));
            }
        }


        private static readonly Lazy<CachedUrlLookupTable> lazy = new Lazy<CachedUrlLookupTable>(() => new());

        public static CachedUrlLookupTable Instance { get { return lazy.Value; } }

        private CachedUrlLookupTable()
        {
        }
    }
}
