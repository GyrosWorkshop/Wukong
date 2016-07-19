using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Wukong.Models;
using Wukong.Options;
using Wukong.Services;

namespace Wukong.Controllers
{
    [Route("api/song")]
    public class SongController : Controller
    {
        private readonly Provider provider;
        public SongController(IOptions<ProviderOption> providerOption)
        {
            provider = new Provider(providerOption.Value.Url);
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchSongRequest query)
        {
            var song = await provider.Search(query);
            return new ObjectResult(song);
        }
    }

}