
namespace Domain.Models
{
    public class CacheKeyInfo
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public TimeSpan? TTL { get; set; }
        public long MemoryUsageBytes { get; set; }
        public bool IsPersistent { get; set; }
    }
}
