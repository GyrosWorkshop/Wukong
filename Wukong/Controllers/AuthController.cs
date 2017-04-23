using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
using System.Threading.Tasks;

namespace Wukong.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {

        [HttpPost]
        public async Task SignIn()
        {
            await HttpContext.Authentication.ChallengeAsync("OpenID", new AuthenticationProperties {RedirectUri = "/"});
        }

        [HttpGet("go/{oAuthProvider}")]
        public IActionResult OAuthChallengeAsync(string oAuthProvider, string redirectUri = "/")
        {
            return new ChallengeResult(oAuthProvider, properties: new AuthenticationProperties() { RedirectUri = redirectUri });
        }
    }
}