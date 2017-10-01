using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using System.Threading.Tasks;

using Wukong.Models;
using System;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Wukong.Options
{
    public class OAuthProviderOptions
    {
        public static GoogleOptions GoogleOAuthOptions(Secret secret)
        {
            return new GoogleOptions
            {
                AuthenticationScheme = "Google",
                DisplayName = "Google",
                ClientId = secret.ClientId,
                ClientSecret = secret.ClientSecret,
                CallbackPath = "/oauth-redirect/google",
                SignInScheme = "Cookies",
                Events = new OAuthEvents
                {
                    OnTicketReceived = context =>
                    {
                        // Cookie expire
                        context.Properties.IsPersistent = true;
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);

                        return Task.FromResult(0);
                    },
                    OnCreatingTicket = (context) =>
                    {
                        var user = context.User;

                        var avatar = user["image"]?.Value<string>("url");
                        if (!string.IsNullOrEmpty(avatar))
                        {
                            // manipulate url and avatar size
                            var avatarUriBuilder = new UriBuilder(avatar)
                            {
                                Query = null
                            };
                            avatar = QueryHelpers.AddQueryString(avatarUriBuilder.ToString(),
                                new Dictionary<string, string> {{"sz", "200"}});

                            // TODO(Leeleo3x): Use all custom claim types or extend existing claim types.
                            context.Identity.AddClaim(new Claim(User.AvatarKey, avatar, ClaimValueTypes.String,
                                context.Options.ClaimsIssuer));
                        }

                        context.Identity.AddClaim(new Claim(ClaimTypes.Authentication, "true", ClaimValueTypes.Boolean,
                            context.Options.ClaimsIssuer));
                        context.Identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod,
                            context.Options.AuthenticationScheme, ClaimValueTypes.String,
                            context.Options.ClaimsIssuer));

                        return Task.FromResult(0);
                    },
                },

            };
        }
    }
}