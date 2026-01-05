using Domain.Enums;
using System.Text.Json.Serialization;

namespace Domain.Records.Requests
{
	public record CreateRedisKeyRequest
    {
        public string Key { get; set; } = "";

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RedisDataType DataType { get; set; } = RedisDataType.String;
        public string Value { get; set; } = "";
        public object? ObjectValue { get; set; }
        public Dictionary<string, string> HashValues { get; set; } = new();
        public List<string> ListValues { get; set; } = new();
        public List<string> SetValues { get; set; } = new();
        public Dictionary<string, double> SortedSetValues { get; set; } = new();
        public int? TtlSeconds { get; set; }
    }
}
