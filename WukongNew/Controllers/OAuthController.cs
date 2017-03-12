using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
using System.Linq;
using Wukong.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Wukong.Controllers
{
    [Route("oauth")]
    public class OAuthController : Controller
    {

        [HttpGet("all")]
        public List<OAuthMethod> AllSchemes()
        {
            var methods = new List<OAuthMethod>();
            foreach (var type in HttpContext.Authentication.GetAuthenticationSchemes())
            {
                if (type.DisplayName != null)
                {
                    methods.Add(new OAuthMethod
                    {
                        Scheme = type.AuthenticationScheme,
                        DisplayName = type.DisplayName,
                        Url = "/oauth/go/" + type.AuthenticationScheme
                    });
                }
            }
            return methods;
        }

        [HttpGet("go/{oAuthProvider}")]
        public IActionResult OAuthChallengeAsync(string oAuthProvider, string redirectUri = "/")
        {
            return new ChallengeResult(oAuthProvider, properties: new AuthenticationProperties() { RedirectUri = redirectUri });
        }
    }
}