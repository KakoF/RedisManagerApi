using StackExchange.Redis;

namespace Infrastructure.Abstracts
{
    public abstract class RedisSaveStrategy
    {
        public abstract string Prefix { get; }
        public abstract Task SaveAsync(IDatabase db, string key, object value, TimeSpan expiry);
        protected string BuildKey(string key) => $"{Prefix}:{key}";
    }
}
