using Domain.Utils;
using Infrastructure.Abstracts;
using StackExchange.Redis;

namespace Infrastructure.Strategy
{
    public class BoolSaveStrategy : RedisSaveStrategy
    {
		public override string Prefix => "Toggle";

		public override async Task SaveAsync(IDatabase db, string key, object value, TimeSpan expiry)
        {
            bool boolValue = JsonElementConverter.ConvertTo<bool>(value);

            await db.StringSetAsync($"{Prefix}:{key}", boolValue, (Expiration)expiry);
        }

		
	}

}
