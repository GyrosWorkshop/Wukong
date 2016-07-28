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
        private readonly IProvider Provider;
        public SongController(IProvider provider)
        {
            Provider = provider;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchSongRequest query)
        {
            var song = await Provider.Search(query);
            return new ObjectResult(song);
        }
    }

}