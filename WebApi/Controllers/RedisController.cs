using Domain.Interfaces;
using Domain.Models;
using Domain.Records.Requests;
using Domain.Records.Response;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RedisController : ControllerBase
    {

        private readonly ICacheRepository _redis;

        public RedisController(ICacheRepository redis)
        {
            _redis = redis;
        }

        [HttpGet("{key}")]
        [ProducesResponseType(typeof(RedisKeyModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetKey(string key)
        {
            try
            {
                var result = await _redis.GetKeyDetailsAsync(key);

                if (!result.Exists)
                    return NotFound(new { key, message = "Key not found" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET api/redis - Listar chaves com filtro
        [HttpGet]
        [ProducesResponseType(typeof(RedisKeysListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKeys(
            [FromQuery] string pattern = "*",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Limitar pageSize por segurança
                pageSize = Math.Min(pageSize, 100);

                var result = await _redis.GetKeysByPatternAsync(pattern, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST api/redis - Criar/Atualizar chave
        [HttpPost]
        [ProducesResponseType(typeof(RedisKeyModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrUpdateKey([FromBody] CreateRedisKeyRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Key))
                    return BadRequest(new { error = "Key is required" });

                if (request.Value == null)
                    return BadRequest(new { error = "Value is required" });

                var result = await _redis.CreateOrUpdateKeyAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT api/redis/{key}/ttl - Atualizar TTL
        [HttpPut("{key}/ttl")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateKeyTtl(
            string key,
            [FromBody] UpdateTtlRequest ttlRequest)
        {
            try
            {
                var success = await _redis.UpdateKeyTtlAsync(key, ttlRequest.TtlSeconds);

                return Ok(new
                {
                    key,
                    success,
                    message = success ? "TTL updated" : "Key not found or TTL not changed",
                    ttlSeconds = ttlRequest.TtlSeconds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE api/redis/{key} - Remover chave
        [HttpDelete("{key}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteKey(string key)
        {
            try
            {
                var deleted = await _redis.DeleteKeyAsync(key);

                return Ok(new
                {
                    key,
                    deleted,
                    message = deleted ? "Key deleted" : "Key not found"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST api/redis/delete-by-pattern - Remover múltiplas chaves
        [HttpPost("delete-by-pattern")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteKeysByPattern([FromBody] DeleteByPatternRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Pattern))
                    return BadRequest(new { error = "Pattern is required" });

                // Para padrões muito amplos, requer confirmação
                if (request.Pattern == "*" && !request.Force)
                    return BadRequest(new
                    {
                        error = "Use force=true to delete all keys",
                        warning = "This will delete ALL keys in the database"
                    });

                var deleted = await _redis.DeleteKeysByPatternAsync(request.Pattern);

                return Ok(new
                {
                    pattern = request.Pattern,
                    deleted,
                    message = deleted ? "Keys deleted" : "No keys matched the pattern"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Models auxiliares
        public class UpdateTtlRequest
        {
            public int TtlSeconds { get; set; }
        }

        public class DeleteByPatternRequest
        {
            public string Pattern { get; set; } = "";
            public bool Force { get; set; }
        }

        /*[HttpGet("{key}")]
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
        }*/
    }
}
