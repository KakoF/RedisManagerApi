using StackExchange.Redis;

namespace Infrastructure
{
    public static class RedisConnectionFactory
    {
        public static IConnectionMultiplexer Create(string connectionString)
        {
            return ConnectionMultiplexer.Connect(connectionString);
        }
    }

}
