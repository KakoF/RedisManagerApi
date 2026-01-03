using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RedisController : ControllerBase
    {

        private readonly IRedisRepository _redis;

        public RedisController(IRedisRepository redis)
        {
            _redis = redis;
        }

        [HttpGet]
        public async Task<IActionResult> GetKeys()
        {
            var keys = await _redis.GetAllKeysWithTTLsAsync();

            return Ok(keys);
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetValue(string key)
        {
            var redisKey = await _redis.GetValueAsync(key);
            return Ok(redisKey);
        }

        [HttpPost]
        public async Task<IActionResult> SetValue(string key, string value, int ttlSeconds = 0)
        {
            await _redis.SetValueAsync(key, value, ttlSeconds);
            return Ok();
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> DeleteKey(string key)
        {
            await _redis.DeleteKeyAsync(key);
            return Ok();
        }
    }
}
