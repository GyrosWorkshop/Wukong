using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;

namespace Wukong.Controllers
{
    [Route("oauth")]
    public class OAuthController : Controller
    {
        [HttpGet("google")]
        public IActionResult GoogleAsync(string redirectUri = "/api/user/login")
        {
            return new ChallengeResult("Google", properties: new AuthenticationProperties() { RedirectUri = redirectUri });
        }
    }

}