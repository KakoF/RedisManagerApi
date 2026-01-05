using Domain.Utils;
using Infrastructure.Abstracts;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Strategy
{
    public class IntSaveStrategy : RedisSaveStrategy
    {
		public override string Prefix => "Integer";

		public override async Task SaveAsync(IDatabase db, string key, object value, TimeSpan expiry)
        {
            int intValue = JsonElementConverter.ConvertTo<int>(value);
            await db.StringSetAsync($"{Prefix}:{key}", intValue, (Expiration)expiry);
        }
    }

}
