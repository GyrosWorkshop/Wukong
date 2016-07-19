using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using System.Threading.Tasks;

using Wukong.Models;

namespace Wukong.Options
{
    public class OAuthOptions
    {
        static public GoogleOptions GoogleOAuthOptions(string clientId, string clientSecret)
        {
            return new GoogleOptions
            {
                AuthenticationScheme = "Google",
                ClientId = clientId,
                ClientSecret = clientSecret,
                CallbackPath = "/oauth-redirect/google",
                SignInScheme = "Cookies",
                Events = new OAuthEvents
                {
                    OnCreatingTicket = (context) =>
                    {
                        var user = context.User;


                        var userId = user.Value<string>("id");
                        if (!string.IsNullOrEmpty(userId))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var userName = user.Value<string>("displayName");
                        if (!string.IsNullOrEmpty(userName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, userName, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var avatar = user["image"]?.Value<string>("url");
                        if (!string.IsNullOrEmpty(avatar))
                        {
                            // TODO(Leeleo3x): Use all custom claim types or extend existing claim types.
                            context.Identity.AddClaim(new Claim(User.AvartaKey, avatar, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        context.Identity.AddClaim(new Claim(ClaimTypes.Authentication, "true", ClaimValueTypes.Boolean, context.Options.ClaimsIssuer));
                        return Task.FromResult(0);
                    },
                },

            };
        }
    }
}