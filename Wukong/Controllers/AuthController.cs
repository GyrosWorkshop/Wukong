using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
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
            return HttpContext.Authentication.GetAuthenticationSchemes()
                .Where(it => it.DisplayName != null)
                .Select(type => new OAuthMethod()
                {
                    Scheme = "Microsoft",
                    DisplayName = type.DisplayName,
                    Url = $"/oauth/go/{type.AuthenticationScheme}"
                });
        }

        [HttpGet("go/{any}")]
        public async Task SignIn(string any, string redirectUri = "/")
        {
            await HttpContext.Authentication.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme, 
                new AuthenticationProperties {RedirectUri = redirectUri});
        }

        [HttpGet("signout")]
        public ActionResult SignOut(string redirectUrl = "/")
        {
            return SignOut(new AuthenticationProperties {RedirectUri = redirectUrl}, 
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}