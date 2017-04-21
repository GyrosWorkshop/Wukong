using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
using System.Linq;
using Wukong.Models;
using System.Collections.Generic;

namespace Wukong.Controllers
{
    [Route("oauth")]
    public class OAuthController : Controller
    {

        [HttpGet("all")]
        public IEnumerable<OAuthMethod> AllSchemes()
        {
            return HttpContext.Authentication.GetAuthenticationSchemes()
                .Where(it => it.DisplayName != null)
                .Select(type => new OAuthMethod()
                {
                    Scheme = type.AuthenticationScheme,
                    DisplayName = type.DisplayName,
                    Url = $"/oauth/go/{type.AuthenticationScheme}"
                });
        }

        [HttpGet("go/{oAuthProvider}")]
        public IActionResult OAuthChallengeAsync(string oAuthProvider, string redirectUri = "/")
        {
            return new ChallengeResult(oAuthProvider, properties: new AuthenticationProperties() { RedirectUri = redirectUri });
        }
    }
}