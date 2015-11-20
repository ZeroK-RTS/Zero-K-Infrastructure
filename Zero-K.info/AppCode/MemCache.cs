using System;
using System.Collections.Concurrent;

namespace ZeroKWeb
{
    public static class MemCache
    {
        readonly static ConcurrentDictionary<string, CacheEntry> cachedData = new ConcurrentDictionary<string, CacheEntry>();
        
        /// <summary>
        /// Returns or generates cached entry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">key value to store at</param>
        /// <param name="initializer">function which creates/updates the value</param>
        /// <param name="seconds">cache timeout in seconds</param>
        /// <returns></returns>
        public static T GetCached<T>(string key, Func<T> initializer, int seconds) {
            var ret = cachedData.AddOrUpdate(
                key,
                k => new CacheEntry(initializer()),
                (k, cur) => DateTime.UtcNow.Subtract(cur.Stored).TotalSeconds < seconds ? cur : new CacheEntry(initializer()));
            return (T)ret.Data;
        }

        class CacheEntry
        {
            public readonly object Data;
            public readonly DateTime Stored = DateTime.UtcNow;

            public CacheEntry(object value) {
                Data = value;
            }
        }
    }
}