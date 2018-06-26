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
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = async (context) =>
                    {
                        context.HttpContext.Response.StatusCode = 401;
                        await context.HttpContext.Response.WriteAsync("Unauthorized");
                    }
                };
            };

        public static JwtBearerOptions JwtBearerOptions(AzureAdB2COptions options, AzureAdB2CPolicies policies)
        {
            return new JwtBearerOptions
            {
                Authority = $"{options.Instance}/{options.Tenant}/{policies.WebSignin}/v2.0",
                Audience = options.ClientId,
            };
        }
    }
}