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
        private readonly IProvider _provider;
        public SongController(IProvider provider)
        {
            _provider = provider;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchSongRequest query)
        {
            var song = await _provider.Search(query);
            return new ObjectResult(song);
        }
    }

}