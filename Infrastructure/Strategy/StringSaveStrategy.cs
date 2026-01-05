using Domain.Utils;
using Infrastructure.Abstracts;
using StackExchange.Redis;

namespace Infrastructure.Strategy
{
	public class StringSaveStrategy : RedisSaveStrategy
    {
		public override string Prefix => "String";

		public override async Task SaveAsync(IDatabase db, string key, object value, TimeSpan expiry)
        {
            string stringValue = JsonElementConverter.ConvertTo<string>(value);

            await db.StringSetAsync($"{Prefix}:{key}", stringValue.ToString(), (Expiration)expiry);
        }

	}

}
