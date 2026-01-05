using Domain.Models;
using Domain.Records.Requests;

namespace Domain.Interfaces
{
    public interface IRedisRepository
    {
        Task<IEnumerable<KeyModel>> GetAsync(FilterKeys filter);
        Task<KeyValueModel> GetAsync(string key);
        Task SetAsync(CreateKeyValue request);
        Task DeleteAsync(string key);
    }
}
