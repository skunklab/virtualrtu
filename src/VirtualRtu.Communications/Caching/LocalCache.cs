using System;
using System.Runtime.Caching;

namespace VirtualRtu.Communications.Caching
{
    public class LocalCache
    {
        private readonly MemoryCache cache;
        private readonly string name;

        public LocalCache(string name)
        {
            this.name = name;
            cache = MemoryCache.Default;
        }

        public object this[string key]
        {
            get => cache[CreateNamedKey(key)];
            set => cache[CreateNamedKey(key)] = value;
        }

        public event EventHandler<CacheItemExpiredEventArgs> OnExpired;

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
            return (T) cache.Get(CreateNamedKey(key));
        }


        private CacheItemPolicy GetCachePolicy(double expirySeconds)
        {
            return new CacheItemPolicy
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