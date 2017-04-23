using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;

namespace Wukong.Options
{
    public class AzureOpenIdConnectionOptions
    {
        public static OpenIdConnectOptions Options(AzureAdB2COptions option)
        {
            return new OpenIdConnectOptions
            {
                AuthenticationScheme = "OpenID",
                ClientId =  option.ClientId,
                ClientSecret = option.ClientSecret,
                CallbackPath = "/auth",
                MetadataAddress = "",
                SignInScheme = "Cookies",
                UseTokenLifetime = true,
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name"
                },
                Events = new OpenIdConnectEvents
                {
                    OnAuthorizationCodeReceived = async context =>
                    {
                        await Task.FromResult(0);
                    },
                    OnMessageReceived = async context =>
                    {
                        await Task.FromResult(0);
                    },

                }
            };
        }
    }
}