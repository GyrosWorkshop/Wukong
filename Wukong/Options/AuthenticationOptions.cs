using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Wukong.Store;

namespace Wukong.Options
{
    public class AuthenticationOptions
    {
        public static CookieAuthenticationOptions CookieAuthenticationOptions(string redisConnection)
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
            return new CookieAuthenticationOptions
            {
                SessionStore = ticketStore,
                LoginPath = "/oauth/login",
                ExpireTimeSpan = TimeSpan.FromDays(30),
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = async (context) =>
                    {
                        context.HttpContext.Response.StatusCode = 401;
                        await context.HttpContext.Response.WriteAsync("Unauthorized");
                    }
                }
            };
        }

        public static JwtBearerOptions JwtBearerOptions(AzureAdB2COptions options, AzureAdB2CPolicies policies)
        {
            return new JwtBearerOptions
            {
                AutomaticChallenge = false,
                AutomaticAuthenticate = true,
                Authority = $"{options.Instance}/{options.Tenant}/{policies.WebSignin}/v2.0",
                Audience = options.ClientId,
            };
        }
    }
}