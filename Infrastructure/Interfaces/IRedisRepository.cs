namespace Infrastructure.Interfaces
{
    public interface IRedisRepository
    {
        Task<IEnumerable<string>> GetKeysAsync();
        Task<List<object>> GetAllKeysWithTTLsAsync();
        Task<string?> GetValueAsync(string key);
        Task<TimeSpan?> GetTTLAsync(string key);
        Task SetValueAsync(string key, string value, int ttlSeconds = 0);
        Task DeleteKeyAsync(string key);
    }

}
