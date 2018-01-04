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
        public static List<OpenIdConnectOptions> Options(AzureAdB2COptions option, string[] policies)
        {
            return policies.Select(s => new OpenIdConnectOptions
            {
                AutomaticChallenge = false,
                ClientId =  option.ClientId,
                ResponseType = OpenIdConnectResponseType.IdToken,
                Authority = $"{option.Instance}/{option.Tenant}/{s}/v2.0",
                SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme,
                GetClaimsFromUserInfoEndpoint = true,
                UseTokenLifetime = true,
                Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = async context =>
                    {
                        context.Properties.IsPersistent = true;
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
                        await Task.FromResult(0);
                    },

                }

            }).ToList();
        }
    }
}