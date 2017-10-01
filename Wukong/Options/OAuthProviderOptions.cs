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
                            avatar = QueryHelpers.AddQueryString(avatarUriBuilder.ToString(), new Dictionary<string, string> { { "sz", "200" } });
                            
                            // TODO(Leeleo3x): Use all custom claim types or extend existing claim types.
                            context.Identity.AddClaim(new Claim(User.AvatarKey, avatar, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        context.Identity.AddClaim(new Claim(ClaimTypes.Authentication, "true", ClaimValueTypes.Boolean, context.Options.ClaimsIssuer));
                        context.Identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, context.Options.AuthenticationScheme, ClaimValueTypes.String, context.Options.ClaimsIssuer));

                        return Task.FromResult(0);
                    },
                },

            };
        }

        public static OAuthOptions GitHubOAuthOptions(Secret secret)
        {
            return new OAuthOptions
            {
                AuthenticationScheme = "GitHub",
                DisplayName = "GitHub",
                ClientId = secret.ClientId,
                ClientSecret = secret.ClientSecret,
                CallbackPath = "/oauth-redirect/github",
                Scope = { "user:email" },
                SignInScheme = "Cookies",
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                UserInformationEndpoint = "https://api.github.com/user",
                SaveTokens = true,
                Events = new OAuthEvents
                {
                    OnTicketReceived = context =>
                    {
                        // Cookie expire
                        context.Properties.IsPersistent = true;
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);

                        return Task.FromResult(0);
                    },
                    OnCreatingTicket = async context =>
                    {
                        // Get the GitHub user
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                        var userId = user.Value<string>("id");
                        if (!string.IsNullOrEmpty(userId))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var userName = user.Value<string>("name");
                        if (!string.IsNullOrEmpty(userName))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Name, userName, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var avatar = user.Value<string>("avatar_url");
                        if (!string.IsNullOrEmpty(avatar))
                        {
                            context.Identity.AddClaim(new Claim(User.AvatarKey, avatar, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var url = user.Value<string>("html_url");
                        if (!string.IsNullOrEmpty(url))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Uri, url, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        context.Identity.AddClaim(new Claim(ClaimTypes.Authentication, "true", ClaimValueTypes.Boolean, context.Options.ClaimsIssuer));
                        context.Identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, context.Options.AuthenticationScheme, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                    }
                }
            };
        }

        public static MicrosoftAccountOptions MicrosoftOAuthOptions(Secret secret)
        {
            return new MicrosoftAccountOptions
            {
                AuthenticationScheme = "Microsoft",
                DisplayName = "Microsoft",
                ClientId = secret.ClientId,
                ClientSecret = secret.ClientSecret,
                CallbackPath = "/oauth-redirect/microsoft",
                SaveTokens = true,
                Events = new OAuthEvents
                {
                    OnTicketReceived = context =>
                    {
                        // Cookie expire
                        context.Properties.IsPersistent = true;
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);

                        return Task.FromResult(0);
                    },
                    OnCreatingTicket = context =>
                    {
                        var user = context.User;
                        var userId = user.Value<string>("id");
                        var avatar = string.Format("https://apis.live.net/v5.0/{0}/picture", userId);
                        context.Identity.AddClaim(new Claim(User.AvatarKey, avatar, ClaimValueTypes.String, context.Options.ClaimsIssuer));

                        context.Identity.AddClaim(new Claim(ClaimTypes.Authentication, "true", ClaimValueTypes.Boolean, context.Options.ClaimsIssuer));
                        context.Identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, context.Options.AuthenticationScheme, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        return Task.FromResult(0);
                    }
                }
            };
        }
    }
}