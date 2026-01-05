using Domain.Models;
using Domain.Records.Requests;
using Domain.Records.Response;

namespace Domain.Interfaces
{
    public interface ICacheRepository
    {
        Task<RedisKeyModel> GetKeyDetailsAsync(string key);
        Task<RedisKeysListResponse> GetKeysByPatternAsync(string pattern, int page = 1, int pageSize = 50);
        Task<RedisKeyModel> CreateOrUpdateKeyAsync(CreateRedisKeyRequest request);
        Task<bool> DeleteKeyAsync(string key);
        Task<bool> DeleteKeysByPatternAsync(string pattern);
        Task<bool> UpdateKeyTtlAsync(string key, int ttlSeconds);
        //Task<Dictionary<string, object>> GetRedisInfoAsync();


        /*Task<T> GetAsync<T>(string key);
        Task SetAsync(CreateRedisKeyRequest request);
        //Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
        Task<Dictionary<string, CacheKeyInfo>> GetKeysAsync(FilterKeys filter);
        Task<long> GetKeyMemoryUsageAsync(string key);
        Task<bool> DeleteKeysByPattern(string pattern);*/
        /*Task<IEnumerable<KeyModel>> GetAsync(FilterKeys filter);
        Task<KeyValueModel> GetAsync(string key);
        Task SetAsync(CreateKeyValue request);
        Task DeleteAsync(string key);*/
    }
}
