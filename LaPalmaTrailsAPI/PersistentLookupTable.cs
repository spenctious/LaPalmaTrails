using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;


namespace LaPalmaTrailsAPI
{
    public class PersistentLookupTable
    {
        private ConcurrentDictionary<string, string> _urlMap = new();
        private readonly object _urlLookupTableLock = new();
        public const string UrlTableLookupFileName = "urlLookupTable.txt";

        private string NotFound { get; set; } = "Not found";

        public void Clear()
        {
            _urlMap.Clear();
        }

        public int Count { get { return _urlMap.Count;} }

        public void TryAdd(string key, string value)
        {
            _urlMap.TryAdd(key, value);
        }

        public string GetValue(string key)
        {
            string? lookupValue = null;
            _urlMap.TryGetValue(key, out lookupValue);
            return lookupValue ?? NotFound;
        }

        public bool ContainsKey(string key)
        {
            return _urlMap.ContainsKey(key);
        }

        /// <summary>
        /// Attempts to restore the URL lookup table from file
        /// </summary>
        /// <returns>true if the lookup table was populated from file, false otherwise</returns>
        public bool LoadUrlLookupTable()
        {
            lock (_urlLookupTableLock)
            {
                // no file to load
                if (!File.Exists(UrlTableLookupFileName)) return false;

                // deserialize from JSON file and check the read worked
                var dictionary = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>
                    (File.ReadAllText(UrlTableLookupFileName));
                if (dictionary == null) return false;

                // repopulate the map
                _urlMap = new ConcurrentDictionary<string, string>(dictionary);
                return true;
            }
        }


        /// <summary>
        /// Save the URL lookup map to file.
        /// Call whenever additional lookups have had to be made
        /// </summary>
        public void SaveUrlLookupTable()
        {
            lock (_urlLookupTableLock)
            {
                // overwrites file if it already exists or creates it new if it doesn't
                File.WriteAllText(UrlTableLookupFileName, JsonConvert.SerializeObject(_urlMap));
            }
        }
    }
}
