using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Models;
using Domain.Records.Requests;
using Infrastructure.Strategy;
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

        public async Task<IEnumerable<KeyModel>> GetAsync(FilterKeys filter)
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());
            var db = _redis.GetDatabase();

            var keys = server.Keys(pattern: $"{filter.Prefix}", pageSize: filter.PageSize);

            var tasks = keys.Select(async key =>
            {
                var ttl = await db.KeyTimeToLiveAsync(key).ConfigureAwait(false);
                return KeyModel.Create(key.ToString(), ttl?.TotalSeconds);
            });

            var result = await Task.WhenAll(tasks);
            return result;

        }



        public async Task<KeyValueModel> GetAsync(string key)
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (!value.HasValue)
                throw new DomainException($"{key} not found");

            var ttl = await db.KeyTimeToLiveAsync(key);
            return KeyValueModel.Create(key, value, ttl?.TotalSeconds);
        }

        
        public async Task SetAsync(CreateKeyValue request)
        {
            var db = _redis.GetDatabase();
            var strategy = RedisSaveStrategyFactory.GetStrategy(request.Value);
            TimeSpan expiry = TimeSpan.FromSeconds(request.TtlSeconds);
            await strategy.SaveAsync(db, request.Key, request.Value, expiry);
        
        }

        public async Task DeleteAsync(string key)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
    }
}
