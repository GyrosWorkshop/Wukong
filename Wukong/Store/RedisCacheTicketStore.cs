using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Wukong.Store
{
    public class RedisCacheTicketStore : ITicketStore
    {
        private IDistributedCache _cache;

        private const string KeyPrefix = "AuthSessionStore";

        public RedisCacheTicketStore(string RedisConnection)
        {
            InitRedisConnectionAsync(RedisConnection);
        }

        async private void InitRedisConnectionAsync(string RedisConnection)
        {
            var host = RedisConnection.Split(':')[0];
            var ipAddress = host == "localhost" ? "localhost" : (await Dns.GetHostAddressesAsync(host))[0].ToString();
            var resolvedRedisConnection = ipAddress + (RedisConnection.Split(':').Length == 2 ? ":" + RedisConnection.Split(':')[1] : "");

            _cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = resolvedRedisConnection,
                InstanceName = "master"
            });
        }

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var guid = Guid.NewGuid();
            var key = KeyPrefix + guid;
            RenewAsync(key, ticket);
            return Task.FromResult(key);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            var options = new DistributedCacheEntryOptions();
            var expiresUtc = ticket.Properties.ExpiresUtc;
            if (expiresUtc.HasValue)
            {
                options.SetAbsoluteExpiration(expiresUtc.Value);
            }
            
            options.SetSlidingExpiration(TimeSpan.FromDays(30));

            _cache.Set(key, SerializeToBytes(ticket), new DistributedCacheEntryOptions());

            return Task.FromResult(0);
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            var ticket = DeserializeFromBytes(_cache.Get(key));
            return Task.FromResult(ticket);
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.FromResult(0);
        }

        private static byte[] SerializeToBytes(AuthenticationTicket source)
        {
            return TicketSerializer.Default.Serialize(source);
        }

        private static AuthenticationTicket DeserializeFromBytes(byte[] source)
        {
            return source == null ? null : TicketSerializer.Default.Deserialize(source);
        }
    }
}
