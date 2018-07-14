using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Wukong.Store;

namespace Wukong.Options
{
    public class AuthenticationOptions
    {
        public static Action<CookieAuthenticationOptions> CookieAuthenticationOptions(string redisConnection)
            => options =>
            {
                ITicketStore ticketStore;
                if (!string.IsNullOrEmpty(redisConnection))
                {
                    ticketStore = new RedisCacheTicketStore(redisConnection);
                }
                else
                {
                    ticketStore = new MemoryCacheStore();
                }
                options.SessionStore = ticketStore;
                options.LoginPath = "/oauth/login";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.Cookie.Expiration = TimeSpan.FromDays(30);
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
            };
    }
}