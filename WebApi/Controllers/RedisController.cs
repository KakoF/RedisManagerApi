using Domain.Interfaces;
using Domain.Models;
using Domain.Records.Requests;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IEnumerable<KeyModel>> GetAsync([FromQuery] FilterKeys filter)
        {
            return await _redis.GetAsync(filter);
        }

        [HttpGet("{key}")]
        public async Task<KeyValueModel> GetValue(string key)
        {
            return await _redis.GetAsync(key);
        }

        [HttpPost]
        public async Task<IActionResult> SetValue([FromBody] CreateKeyValue request)
        {
            await _redis.SetAsync(request);
            return Ok();
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> DeleteKey(string key)
        {
            await _redis.DeleteAsync(key);
            return Ok();
        }
    }
}
