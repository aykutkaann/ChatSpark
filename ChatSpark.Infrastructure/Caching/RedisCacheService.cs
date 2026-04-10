using ChatSpark.Application.Abstractions;
using ChatSpark.Infrastructure.Persistence;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ChatSpark.Infrastructure.Caching
{
    public class RedisCacheService :ICacheService
    {
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }
        
        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {

            var value = await _db.StringGetAsync(key);

            if(value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString());


        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(value);

            await _db.StringSetAsync(key, json, expiry);
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}
