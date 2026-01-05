using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Models;
using Domain.Records.Requests;
using Domain.Records.Response;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

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
            var model = RedisKeyModel.Init(key);

            // Verifica se a chave existe
            var keyExists = await _redisDb.KeyExistsAsync(key);

            if (!keyExists)
                throw new DomainException("Key not Found");

            // Obtém o tipo da chave
            var keyType = await _redisDb.KeyTypeAsync(key);
            model.SetType(keyType.ToString());

            // Obtém TTL
            var ttl = await _redisDb.KeyTimeToLiveAsync(key);
            model.SetExpirations(ttl);

            // Obtém valor baseado no tipo
            model.SetValue(await GetValueByTypeAsync(key, keyType));

            // Tenta obter uso de memória (Redis >= 4.0)
            var memoryResult = await _redisDb.ExecuteAsync("MEMORY", "USAGE", key);
            if (!memoryResult.IsNull)
                model.SetMemoryUsageBytes((long)memoryResult);

            return model;
        }

        private async Task<string?> GetValueByTypeAsync(string key, RedisType keyType)
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
                    details.SetValue(details.Value[..500] + "... [truncated]");
                }
                return details;
            });

            response.Keys = (await Task.WhenAll(tasks)).ToList();

            return response;
        }

        /*public async Task<RedisKeyModel> CreateOrUpdateKeyAsync(CreateRedisKeyRequest request)
        {
            var model = RedisKeyModel.Init(request.Key);
            bool setSuccess;

            if (request.TtlSeconds.HasValue && request.TtlSeconds > 0)
            {
                // Define com TTL
                setSuccess = await _redisDb.StringSetAsync(
                    request.Key,
                    request.Value,
                    TimeSpan.FromSeconds(request.TtlSeconds.Value));
                model.SetExpirations(TimeSpan.FromSeconds(request.TtlSeconds.Value));
            }
            else
            {
                // Define sem expiração
                setSuccess = await _redisDb.StringSetAsync(request.Key, request.Value);
            }

            if (setSuccess)
            {
                model.SetValue(request.Value);
                model.SetType("String");

                _logger.LogInformation("Key {Key} created/updated", request.Key);
            }
            return model;
        }*/

        public async Task<bool> DeleteKeyAsync(string key)
        {
            var deleted = await _redisDb.KeyDeleteAsync(key);
            _logger.LogInformation("Key {Key} deleted: {Deleted}", key, deleted);
            return deleted;

        }

        public async Task<bool> DeleteKeysByPatternAsync(string pattern)
        {
            var keys = _server.Keys(pattern: pattern).ToArray();
            if (keys.Length == 0)
                throw new DomainException("Key not Found");

            var deletedCount = await _redisDb.KeyDeleteAsync(keys);
            _logger.LogInformation("Deleted {Count} keys matching pattern {Pattern}", deletedCount, pattern);
            return deletedCount > 0;
        }

        public async Task<bool> UpdateKeyTtlAsync(string key, int ttlSeconds)
        {
            if (ttlSeconds <= 0)
                return await _redisDb.KeyPersistAsync(key); // Remover TTL (tornar permanente)

            return await _redisDb.KeyExpireAsync(key, TimeSpan.FromSeconds(ttlSeconds)); // Definir novo TTL
        }

        /*public async Task<RedisKeyModel> GetKeyDetailsAsync(string key)
        {
            var model = RedisKeyModel.Init(key);

            // Verifica se a chave existe
            var exists = await _redisDb.KeyExistsAsync(key);
            if (!exists)
                throw new DomainException("Key not Found");

            // Obtém o tipo da chave
            var keyType = await _redisDb.KeyTypeAsync(key);
            model.SetType(keyType.ToString());

            // Busca valor baseado no tipo
            switch (keyType)
            {
                case RedisType.String:
                    var stringValue = await _redisDb.StringGetAsync(key);
                    model.SetValue(stringValue);
                    break;

                case RedisType.List:
                    var listLength = await _redisDb.ListLengthAsync(key);
                    var firstFiveList = await _redisDb.ListRangeAsync(key, 0, 4);
                    model.SetValue($"[List - Length: {listLength}, Sample: {string.Join(", ", firstFiveList)}]");
                    break;

                case RedisType.Hash:
                    var hashEntries = await _redisDb.HashGetAllAsync(key);
                    model.SetValue($"[Hash - Fields: {hashEntries.Length}]");
                    break;

                case RedisType.Set:
                    var setMembers = await _redisDb.SetMembersAsync(key);
                    model.SetValue($"[Set - Count: {setMembers.Length}]");
                    break;

                case RedisType.SortedSet:
                    var sortedSet = await _redisDb.SortedSetRangeByRankWithScoresAsync(key, 0, 4);
                    model.SetValue($"[SortedSet - Sample: {string.Join(", ", sortedSet.Take(3))}]");
                    break;
            }

            // Obtém TTL
            var ttl = await _redisDb.KeyTimeToLiveAsync(key);
            if (ttl.HasValue)
                model.SetExpirations(ttl.Value);

            return model;
        }*/

        public async Task<RedisKeyModel> CreateOrUpdateKeyAsync(CreateRedisKeyRequest request)
        {
            var model = RedisKeyModel.Init(request.Key);
            bool operationSuccess = false;

            switch (request.DataType)
            {
                case RedisDataType.String:
                    operationSuccess = await SetStringAsync(request, model);
                    break;

                case RedisDataType.List:
                    operationSuccess = await SetListAsync(request, model);
                    break;

                case RedisDataType.Hash:
                    operationSuccess = await SetHashAsync(request, model);
                    break;

                case RedisDataType.Set:
                    operationSuccess = await SetSetAsync(request, model);
                    break;

                case RedisDataType.SortedSet:
                    operationSuccess = await SetSortedSetAsync(request, model);
                    break;

                default:
                    throw new ArgumentException($"Tipo de dado não suportado: {request.DataType}");
            }

            if (operationSuccess && request.TtlSeconds.HasValue && request.TtlSeconds > 0)
            {
                await _redisDb.KeyExpireAsync(request.Key, TimeSpan.FromSeconds(request.TtlSeconds.Value));
                model.SetExpirations(TimeSpan.FromSeconds(request.TtlSeconds.Value));
            }

            return model;
        }

        private async Task<bool> SetStringAsync(CreateRedisKeyRequest request, RedisKeyModel model)
        {
            var success = await _redisDb.StringSetAsync(request.Key, request.Value);
            if (success)
            {
                model.SetValue(request.Value);
                model.SetType("String");
            }
            return success;
        }

        private async Task<bool> SetListAsync(CreateRedisKeyRequest request, RedisKeyModel model)
        {
            if (request.ListValues == null || !request.ListValues.Any())
                throw new ArgumentException("ListValues é obrigatório para tipo List");

            // Limpa a lista existente (se houver)
            await _redisDb.KeyDeleteAsync(request.Key);

            // Adiciona todos os itens
            var tasks = request.ListValues.Select(value =>
                _redisDb.ListRightPushAsync(request.Key, value));

            await Task.WhenAll(tasks);

            model.SetValue($"[List - {request.ListValues.Count} items]");
            model.SetType("List");
            return true;
        }

        private async Task<bool> SetHashAsync(CreateRedisKeyRequest request, RedisKeyModel model)
        {
            if (request.HashValues == null || !request.HashValues.Any())
                throw new ArgumentException("HashValues é obrigatório para tipo Hash");

            var entries = request.HashValues
                .Select(kv => new HashEntry(kv.Key, kv.Value))
                .ToArray();

            await _redisDb.HashSetAsync(request.Key, entries);

            model.SetValue($"[Hash - {request.HashValues.Count} fields]");
            model.SetType("Hash");
            return true;
        }

        private async Task<bool> SetSetAsync(CreateRedisKeyRequest request, RedisKeyModel model)
        {
            if (request.SetValues == null || !request.SetValues.Any())
                throw new ArgumentException("SetValues é obrigatório para tipo Set");

            var redisValues = request.SetValues.Select(v => (RedisValue)v).ToArray();
            var added = await _redisDb.SetAddAsync(request.Key, redisValues);

            model.SetValue($"[Set - {added} unique members added]");
            model.SetType("Set");
            return added > 0;
        }

        private async Task<bool> SetSortedSetAsync(CreateRedisKeyRequest request, RedisKeyModel model)
        {
            if (request.SortedSetValues == null || !request.SortedSetValues.Any())
                throw new ArgumentException("SortedSetValues é obrigatório para tipo SortedSet");

            var entries = request.SortedSetValues
                .Select(kv => new SortedSetEntry(kv.Key, kv.Value))
                .ToArray();

            var added = await _redisDb.SortedSetAddAsync(request.Key, entries);

            model.SetValue($"[SortedSet - {added} members with scores]");
            model.SetType("SortedSet");
            return added > 0;
        }

    }
}
