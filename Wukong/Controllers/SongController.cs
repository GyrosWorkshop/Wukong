using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Wukong.Models;
using Wukong.Services;

namespace Wukong.Controllers
{
    [Route("api/song")]
    public class SongController : Controller
    {
        private readonly IProvider provider;
        public SongController(IProvider provider)
        {
            this.provider = provider;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchSongRequest query)
        {
            var song = await provider.Search(query);
            return new ObjectResult(song);
        }
    }

}