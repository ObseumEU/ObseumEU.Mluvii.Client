using System.Runtime.Caching;

namespace ObseumEU.Mluvii.Client.Cache
{
    public interface ICacheService
    {
        T Get<T>(string cacheKey);
        void Set(string cacheKey, object item, double minutes);
        Task<T> GetOrLoadFromCache<T>(Func<Task<T>> callbackDelegate, string key, double minutes);
        T GetOrLoadFromCache<T>(Func<T> callbackDelegate, string key, double minutes);
    }

    public class InMemoryCache : ICacheService
    {
        public T Get<T>(string cacheKey)
        {
            return MemoryCache.Default.Get(cacheKey) is T ? (T)MemoryCache.Default.Get(cacheKey) : default;
        }

        public void Set(string cacheKey, object item, double minutes)
        {
            if (item != null) MemoryCache.Default.Set(cacheKey, item, DateTime.Now.AddMinutes(minutes));
        }

        public async Task<T> GetOrLoadFromCache<T>(Func<Task<T>> callbackDelegate, string key, double minutes)
        {
            T result = Get<T>(key);

            if (result == null || EqualityComparer<T>.Default.Equals(result, default(T)))
            {
                result = await callbackDelegate();
                Set(key, result, minutes);
            }

            return result;
        }

        public T GetOrLoadFromCache<T>(Func<T> callbackDelegate, string key, double minutes)
        {
            T result = Get<T>(key);

            if (result == null)
            {
                result = callbackDelegate();
                Set(key, result, minutes);
            }

            return result;
        }
    }
}