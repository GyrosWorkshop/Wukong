using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Wukong.Services;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Wukong.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ProviderController : Controller
    {

        private readonly IProvider Provider;
        public ProviderController(IProvider provider)
        {
            Provider = provider;
        }

        string UserId => Models.User.GetUserIdentifier(HttpContext.User.FindFirst(ClaimTypes.AuthenticationMethod).Value,
            HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);

        // GET: /<controller>/
        [HttpPost("{feature}")]
        public async Task<IActionResult> Index(string feature, [FromBody] JObject body)
        {
            var result = await Provider.ApiProxy(feature, body);
            if (result != null)
            {
                return new ObjectResult(result);
            }
            else
            {
                return StatusCode(500, "ApiProxy null (feature not exists?)");
            }
        }
    }
}
