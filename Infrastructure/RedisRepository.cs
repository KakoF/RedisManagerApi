using Infrastructure.Interfaces;
using StackExchange.Redis;

namespace Infrastructure
{
    public class RedisRepository : IRedisRepository
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisRepository(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<IEnumerable<string>> GetKeysAsync()
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());
            var keys = server.Keys();
            return keys.Select(k => k.ToString());
        }

        public async Task<List<object>> GetAllKeysWithTTLsAsync()
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());
            var db = _redis.GetDatabase();

            var keys = server.Keys();
            var result = new List<object>();

            foreach (var key in keys)
            {
                var ttl = await db.KeyTimeToLiveAsync(key);
                result.Add(new { Key = key.ToString(), TTL = ttl?.TotalSeconds });
            }

            return result;
        }


        public async Task<string?> GetValueAsync(string key)
        {
            var db = _redis.GetDatabase();
            return await db.StringGetAsync(key);
        }

        public async Task<TimeSpan?> GetTTLAsync(string key)
        {
            var db = _redis.GetDatabase();
            return await db.KeyTimeToLiveAsync(key);
        }

        public async Task SetValueAsync(string key, string value, int ttlSeconds = 0)
        {
            var db = _redis.GetDatabase();
            TimeSpan? expiry = ttlSeconds > 0 ? TimeSpan.FromSeconds(ttlSeconds) : null;
            await db.StringSetAsync(key, value, (Expiration)expiry);
        }

        public async Task DeleteKeyAsync(string key)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
    }
}
