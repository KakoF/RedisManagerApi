using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models
{
	public class RedisKeyModel
	{
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public string? Ttl { get; set; }
        public string Type { get; set; } = "";
        public long? MemoryUsageBytes { get; set; }
        public bool Exists { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
