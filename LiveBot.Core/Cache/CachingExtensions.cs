using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LiveBot.Core.Cache
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCaching(this IServiceCollection services, string instanceName, ConfigurationManager configuration)
        {
            string redisConnectionString = configuration.GetValue<string>(key: "Redis_connectionstring") ?? "";
            var options = ConfigurationOptions.Parse(configuration: redisConnectionString);
            options.ClientName = instanceName;
            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
            services.AddSingleton(connectionMultiplexer);

            return services;
        }
    }

    public static class ConnectionMultiplexerExtensions
    {
        // Default seconds to mark a cached object as expired on AbsoluteExpiration
        private static readonly int _defaultSecondsToExpire = 300; // 5 minutes

        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

#pragma warning disable CS8603 // Possible null reference return.

        /// <summary>
        /// Set a record in the cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redis"></param>
        /// <param name="recordId"></param>
        /// <param name="data"></param>
        /// <param name="expiryTime"></param>
        /// <returns></returns>
        public static async Task SetRecordAsync<T>(this ConnectionMultiplexer redis, string recordId, T data, TimeSpan? expiryTime = null)
        {
            StackExchange.Redis.IDatabase cache = redis.GetDatabase();
            recordId = $"{redis.ClientName}:{recordId}".ToLower();

            expiryTime ??= TimeSpan.FromSeconds(_defaultSecondsToExpire);
            var jsonData = JsonSerializer.Serialize(value: data, options: jsonSerializerOptions);
            await cache.StringSetAsync(key: recordId, value: jsonData, expiry: expiryTime);
        }

        /// <summary>
        /// Get a record from the cache with the type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redis"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public static async Task<T> GetRecordAsync<T>(this ConnectionMultiplexer redis, string recordId)
        {
            StackExchange.Redis.IDatabase cache = redis.GetDatabase();
            recordId = $"{redis.ClientName}:{recordId}".ToLower();

            var jsonData = await cache.StringGetAsync(key: recordId);

            if (jsonData.IsNullOrEmpty || !jsonData.HasValue)
            {
                return default;
            }
            else
            {
                return JsonSerializer.Deserialize<T>(json: jsonData.ToString(), options: jsonSerializerOptions);
            }
        }

        /// <summary>
        /// Set an item in a list in Redis
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redis"></param>
        /// <param name="recordId"></param>
        /// <param name="fieldName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task SetListItemAsync<T>(this ConnectionMultiplexer redis, string recordId, string fieldName, T data)
        {
            StackExchange.Redis.IDatabase cache = redis.GetDatabase();
            recordId = $"{redis.ClientName}:{recordId}".ToLower();

            var jsonData = JsonSerializer.Serialize(value: data, options: jsonSerializerOptions);
            await cache.HashSetAsync(key: recordId, hashField: fieldName, value: jsonData);
        }

        /// <summary>
        /// Get an item from a list in Redis
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redis"></param>
        /// <param name="recordId"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static async Task<T> GetListItemAsync<T>(this ConnectionMultiplexer redis, string recordId, string fieldName)
        {
            StackExchange.Redis.IDatabase cache = redis.GetDatabase();
            recordId = $"{redis.ClientName}:{recordId}".ToLower();

            var recordExists = await cache.HashExistsAsync(key: recordId, hashField: fieldName);
            if (!recordExists)
                return default;

            var jsonData = await cache.HashGetAsync(key: recordId, hashField: fieldName);

            if (jsonData.IsNullOrEmpty || !jsonData.HasValue)
            {
                return default;
            }
            else
            {
                return JsonSerializer.Deserialize<T>(json: jsonData.ToString(), options: jsonSerializerOptions);
            }
        }

        public static async Task<bool> DeleteListItemAsync(this ConnectionMultiplexer redis, string recordId, string fieldName)
        {
            StackExchange.Redis.IDatabase cache = redis.GetDatabase();
            recordId = $"{redis.ClientName}:{recordId}".ToLower();

            return await cache.HashDeleteAsync(key: recordId, hashField: fieldName);
        }

        public static async Task<long> DeleteListAsync(this ConnectionMultiplexer redis, string recordId)
        {
            StackExchange.Redis.IDatabase cache = redis.GetDatabase();
            recordId = $"{redis.ClientName}:{recordId}".ToLower();

            var hashFields = await cache.HashKeysAsync(key: recordId);
            return await cache.HashDeleteAsync(key: recordId, hashFields: hashFields);
        }

        /// <summary>
        /// Obtain a lock for a record
        /// </summary>
        /// <param name="redis"></param>
        /// <param name="recordId"></param>
        /// <param name="identifier"></param>
        /// <param name="expiryTime"></param>
        /// <returns></returns>
        public static async Task<bool> ObtainLockAsync(this ConnectionMultiplexer redis, string recordId, Guid identifier, TimeSpan expiryTime)
        {
            StackExchange.Redis.IDatabase cache = redis.GetDatabase();
            string lockRecordId = $"lock:{redis.ClientName}:{recordId}".ToLower();

            return await cache.LockTakeAsync(key: lockRecordId, value: identifier.ToString(), expiry: expiryTime);
        }

        /// <summary>
        /// Release a lock for a record
        /// </summary>
        /// <param name="redis"></param>
        /// <param name="recordId"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static async Task<bool> ReleaseLockAsync(this ConnectionMultiplexer redis, string recordId, Guid identifier)
        {
            StackExchange.Redis.IDatabase cache = redis.GetDatabase();
            string lockRecordId = $"lock:{redis.ClientName}:{recordId}".ToLower();

            return await cache.LockReleaseAsync(key: lockRecordId, value: identifier.ToString());
        }
    }

#pragma warning restore CS8603 // Possible null reference return.
}
