using Domain.Models;

namespace Domain.Records.Response
{
    public class RedisKeysListResponse
    {
        public List<RedisKeyModel> Keys { get; set; } = new();
        public int TotalCount { get; set; }
        public string Pattern { get; set; } = "";
        public DateTime QueriedAt { get; set; }
    }
}
