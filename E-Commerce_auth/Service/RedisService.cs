using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
namespace E_Commerce_auth.Service

{
    public class RedisService
    {
        private readonly IDatabase _redis;

        public RedisService(IDatabase db)
        {
            _redis = db;
        }

        public Task<bool> SetAsync(string key, string value, TimeSpan? ttl = null)
            => _redis.StringSetAsync(key, value, ttl, When.Always);

        public async Task<string?> GetAsync(string key)
        {
            var v = await _redis.StringGetAsync(key);
            return v.IsNullOrEmpty ? null : v.ToString();
        }

        public Task<bool> RemoveAsync(string key)
            => _redis.KeyDeleteAsync(key);

        public Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? ttl = null)
            => _redis.StringSetAsync(key, value, ttl, When.NotExists);

        public Task<long> IncrementAsync(string key)
        {
            return _redis.StringIncrementAsync(key);
        }


    }
}
