using System;
using System.Runtime.Caching;

namespace VirtualRtu.Communications.Caching
{
    public class LocalCache
    {
        public LocalCache(string name)
        {
            this.name = name;
            cache = MemoryCache.Default;            
        }

        public event System.EventHandler<CacheItemExpiredEventArgs> OnExpired;
        private string name;
        private MemoryCache cache;

        public object this[string key]
        {
            get { return cache[CreateNamedKey(key)]; }
            set { cache[CreateNamedKey(key)] = value; }
        }

        public bool Contains(string key)
        {
            return cache.Contains(CreateNamedKey(key));
        }

        public object Remove(string key)
        {
            return cache.Remove(CreateNamedKey(key));
        }

        public bool Add(string key, object value, double expirySeconds)
        {
            return cache.Add(CreateNamedKey(key), value, GetCachePolicy(expirySeconds));
        }

        public object Get(string key)
        {
            return cache.Get(CreateNamedKey(key));
        }

        public T Get<T>(string key)
        {
            return (T)cache.Get(CreateNamedKey(key));
        }


        private CacheItemPolicy GetCachePolicy(double expirySeconds)
        {
            return new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(expirySeconds),
                RemovedCallback = OnRemovedFromCache
            };
        }


        private string CreateNamedKey(string key)
        {
            return $"{name}:key={key}";
        }

        private void OnRemovedFromCache(CacheEntryRemovedArguments args)
        {
            if (args.RemovedReason == CacheEntryRemovedReason.Expired)
            {
                string key = args.CacheItem.Key.Replace($"{name}:key=", "");
                OnExpired?.Invoke(this, new CacheItemExpiredEventArgs(name, key, args.CacheItem.Value));
            }
        }

    }
}
