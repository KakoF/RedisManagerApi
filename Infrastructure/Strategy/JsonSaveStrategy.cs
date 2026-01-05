using Infrastructure.Abstracts;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Strategy
{
    public class JsonSaveStrategy : RedisSaveStrategy
    {
		public override string Prefix => "Json";

		public override async Task SaveAsync(IDatabase db, string key, object value, TimeSpan expiry)
        {
            var json = JsonSerializer.Serialize(value);

            await db.StringSetAsync($"{Prefix}:{key}", json, (Expiration)expiry);
        }

	}

}
