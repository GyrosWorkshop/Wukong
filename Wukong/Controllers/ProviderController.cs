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

        private readonly IProvider provider;
        public ProviderController(IProvider provider)
        {
            this.provider = provider;
        }

        // GET: /<controller>/
        [HttpPost("{feature}")]
        public async Task<IActionResult> Index(string feature, [FromBody] JObject body)
        {
            var result = await provider.ApiProxy(feature, body);
            return result != null ? new ObjectResult(result) : StatusCode(500, "ApiProxy null (feature not exists?)");
        }
    }
}
