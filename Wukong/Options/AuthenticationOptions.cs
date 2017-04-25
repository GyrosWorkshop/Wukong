using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Wukong.Store;

namespace Wukong.Options
{
    public class AuthenticationOptions
    {
        static public CookieAuthenticationOptions CookieAuthenticationOptions(string redisConnection)
        {
            ITicketStore ticketStore;
            if (!String.IsNullOrEmpty(redisConnection))
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
    }
}