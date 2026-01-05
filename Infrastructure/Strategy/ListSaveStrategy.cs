
using Infrastructure.Abstracts;
using StackExchange.Redis;

namespace Infrastructure.Strategy
{
    public class ListSaveStrategy : RedisSaveStrategy
    {
		public override string Prefix => "List";

		public override async Task SaveAsync(IDatabase db, string key, object value, TimeSpan expiry)
        {
            if (value is IEnumerable<object> list)
            {
                foreach (var item in list)
                    await db.ListRightPushAsync(key, item.ToString());
            }
            await db.KeyExpireAsync($"{Prefix}:{key}", expiry);
        }

	}

}
