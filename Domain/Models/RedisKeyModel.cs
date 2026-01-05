
namespace Domain.Models
{
    public class RedisKeyModel
    {
        public string Key { get; private set; }
        public string? Value { get; private set; } = null;
        public string? Ttl { get; private set; }
        public string? Type { get; private set; } = null;
        public long? MemoryUsageBytes { get; private set; }
        public bool Exists { get; private set; }
        public DateTime? ExpiresAt { get; private set; }

        private RedisKeyModel(string key, string? value, string? ttl, string? type, long? memoryUsageBytes, bool exists, DateTime? expiresAt)
        {
            Key = key;
            Value = value;
            Ttl = ttl;
            Type = type;
            MemoryUsageBytes = memoryUsageBytes;
            Exists = exists;
            ExpiresAt = expiresAt;
        }

        public static RedisKeyModel Init(string key)
        {
            return new RedisKeyModel(
                key: key,
                value: null,
                ttl: null,
                type: null,
                memoryUsageBytes: null,
                exists: false,
                expiresAt: null
            );
        }

        public void SetValue(string? value)
        {
            Value = value;
            Exists = true;
        }

        public void SetExpirations(TimeSpan? ttl)
        {
            Ttl = ttl?.ToString(@"dd\.hh\:mm\:ss");
            ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null;
        }
        public void SetType(string type)
        {
            Type = type;
        }
        public void SetMemoryUsageBytes(long? memoryUsageBytes)
        {
            MemoryUsageBytes = memoryUsageBytes;
        }
    }
}
