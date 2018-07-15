using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Wukong.Models;

namespace Wukong.Controllers
{
    [Route("oauth")]
    public class AuthController : Controller
    {
        [HttpGet("all")]
        public IEnumerable<OAuthMethod> AllSchemes()
        {
            return
                new List<OAuthMethod>{ new OAuthMethod()
                {
                    Scheme = "Microsoft",
                    DisplayName = "Microsoft",
                    Url = $"/oauth/go/{OpenIdConnectDefaults.AuthenticationScheme}"
                }};
        }

        [HttpGet("go/{any}")]
        public async Task SignIn(string any, string redirectUri = "/")
        {
            await HttpContext.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties {
                    RedirectUri = redirectUri,
                    IsPersistent = true
                });
        }

        [HttpGet("signout")]
        public SignOutResult SignOut(string redirectUrl = "/")
        {
            return SignOut(new AuthenticationProperties { RedirectUri = redirectUrl },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}