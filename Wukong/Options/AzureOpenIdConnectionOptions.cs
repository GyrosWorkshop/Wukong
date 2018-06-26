using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Wukong.Options
{
    public class AzureOpenIdConnectionOptions
    {
        public static Action<OpenIdConnectOptions> Options(AzureAdB2COptions option, string policy)
            => options =>
            {
                options.ClientId = option.ClientId;
                options.ResponseType = OpenIdConnectResponseType.IdToken;
                options.Authority = $"{option.Instance}/{option.Tenant}/{policy}/v2.0";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.UseTokenLifetime = true;
                options.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = async context =>
                    {
                        context.Properties.IsPersistent = true;
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(999999);
                        context.Properties.AllowRefresh = true;
                        await Task.FromResult(0);
                    },
                };

            };
    }
}