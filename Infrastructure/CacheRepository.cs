using Domain.Interfaces;
using Domain.Models;
using Domain.Records.Requests;
using Domain.Records.Response;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure
{
    public class CacheRepository : ICacheRepository
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _redisDb;
        private readonly IServer _server;
        private readonly ILogger<CacheRepository> _logger;

        public CacheRepository(IDistributedCache distributedCache, IConnectionMultiplexer connectionMultiplexer, ILogger<CacheRepository> logger)
        {
            _distributedCache = distributedCache;
            _connectionMultiplexer = connectionMultiplexer;
            _redisDb = _connectionMultiplexer.GetDatabase();
            _server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            _logger = logger;
        }

        public async Task<RedisKeyModel> GetKeyDetailsAsync(string key)
        {
            var response = new RedisKeyModel { Key = key };

            try
            {
                // Verifica se a chave existe
                response.Exists = await _redisDb.KeyExistsAsync(key);

                if (!response.Exists)
                    return response;

                // Obtém o tipo da chave
                var keyType = await _redisDb.KeyTypeAsync(key);
                response.Type = keyType.ToString();

                // Obtém TTL
                var ttl = await _redisDb.KeyTimeToLiveAsync(key);
                response.Ttl = ttl?.ToString(@"dd\.hh\:mm\:ss");
                response.ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null;

                // Obtém valor baseado no tipo
                response.Value = await GetValueByTypeAsync(key, keyType);

                // Tenta obter uso de memória (Redis >= 4.0)
                try
                {
                    var memoryResult = await _redisDb.ExecuteAsync("MEMORY", "USAGE", key);
                    if (!memoryResult.IsNull)
                        response.MemoryUsageBytes = (long)memoryResult;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "MEMORY USAGE command not available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key details for {Key}", key);
                throw;
            }

            return response;
        }

        private async Task<string> GetValueByTypeAsync(string key, RedisType keyType)
        {
            return keyType switch
            {
                RedisType.String => await _redisDb.StringGetAsync(key),
                RedisType.List => await GetListValueAsync(key),
                RedisType.Set => await GetSetValueAsync(key),
                RedisType.Hash => await GetHashValueAsync(key),
                RedisType.SortedSet => await GetSortedSetValueAsync(key),
                _ => $"[Type not fully supported: {keyType}]"
            };
        }

        private async Task<string> GetListValueAsync(string key)
        {
            var length = await _redisDb.ListLengthAsync(key);
            var firstFive = await _redisDb.ListRangeAsync(key, 0, 4);
            return $"[List - Length: {length}, First 5: {string.Join(", ", firstFive)}]";
        }

        private async Task<string> GetSetValueAsync(string key)
        {
            var members = await _redisDb.SetMembersAsync(key);
            var count = await _redisDb.SetLengthAsync(key);
            return $"[Set - Count: {count}, Sample: {string.Join(", ", members.Take(5))}]";
        }

        private async Task<string> GetHashValueAsync(string key)
        {
            var entries = await _redisDb.HashGetAllAsync(key);
            return $"[Hash - Fields: {entries.Length}, Sample: {string.Join(", ", entries.Take(3).Select(e => $"{e.Name}:{e.Value}"))}]";
        }

        private async Task<string> GetSortedSetValueAsync(string key)
        {
            var firstFive = await _redisDb.SortedSetRangeByRankWithScoresAsync(key, 0, 4);
            return $"[SortedSet - Sample with scores: {string.Join(", ", firstFive.Select(x => $"{x.Element}:{x.Score}"))}]";
        }

        public async Task<RedisKeysListResponse> GetKeysByPatternAsync(string pattern, int page = 1, int pageSize = 50)
        {
            var response = new RedisKeysListResponse
            {
                Pattern = pattern,
                QueriedAt = DateTime.UtcNow
            };

            try
            {
                // Busca todas as chaves correspondentes ao padrão
                var allKeys = _server.Keys(pattern: pattern).Select(k => k.ToString()).ToList();
                response.TotalCount = allKeys.Count;

                // Paginação
                var pagedKeys = allKeys
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Busca detalhes das chaves paginadas (em paralelo para performance)
                var tasks = pagedKeys.Select(async key =>
                {
                    var details = await GetKeyDetailsAsync(key);
                    // Para performance, podemos pegar apenas informações básicas
                    if (details.Type == "String" && details.Value.Length > 500)
                    {
                        details.Value = details.Value[..500] + "... [truncated]";
                    }
                    return details;
                });

                response.Keys = (await Task.WhenAll(tasks)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keys by pattern {Pattern}", pattern);
                throw;
            }

            return response;
        }

        public async Task<RedisKeyModel> CreateOrUpdateKeyAsync(CreateRedisKeyRequest request)
        {
            var response = new RedisKeyModel { Key = request.Key };

            try
            {
                bool setSuccess;

                if (request.TtlSeconds.HasValue && request.TtlSeconds > 0)
                {
                    // Define com TTL
                    setSuccess = await _redisDb.StringSetAsync(
                        request.Key,
                        request.Value,
                        TimeSpan.FromSeconds(request.TtlSeconds.Value));

                    response.Ttl = TimeSpan.FromSeconds(request.TtlSeconds.Value)
                        .ToString(@"dd\.hh\:mm\:ss");
                    response.ExpiresAt = DateTime.UtcNow.AddSeconds(request.TtlSeconds.Value);
                }
                else
                {
                    // Define sem expiração
                    setSuccess = await _redisDb.StringSetAsync(request.Key, request.Value);
                }

                if (setSuccess)
                {
                    response.Exists = true;
                    response.Type = "String";
                    response.Value = request.Value;

                    _logger.LogInformation("Key {Key} created/updated", request.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating key {Key}", request.Key);
                throw;
            }

            return response;
        }

        public async Task<bool> DeleteKeyAsync(string key)
        {
            try
            {
                var deleted = await _redisDb.KeyDeleteAsync(key);
                _logger.LogInformation("Key {Key} deleted: {Deleted}", key, deleted);
                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key {Key}", key);
                throw;
            }
        }

        public async Task<bool> DeleteKeysByPatternAsync(string pattern)
        {
            try
            {
                var keys = _server.Keys(pattern: pattern).ToArray();
                if (keys.Length == 0)
                    return false;

                var deletedCount = await _redisDb.KeyDeleteAsync(keys);
                _logger.LogInformation("Deleted {Count} keys matching pattern {Pattern}", deletedCount, pattern);
                return deletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting keys by pattern {Pattern}", pattern);
                throw;
            }
        }

        public async Task<bool> UpdateKeyTtlAsync(string key, int ttlSeconds)
        {
            try
            {
                if (ttlSeconds <= 0)
                {
                    // Remover TTL (tornar permanente)
                    return await _redisDb.KeyPersistAsync(key);
                }
                else
                {
                    // Definir novo TTL
                    return await _redisDb.KeyExpireAsync(key, TimeSpan.FromSeconds(ttlSeconds));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating TTL for key {Key}", key);
                throw;
            }
        }

       
    }
}
