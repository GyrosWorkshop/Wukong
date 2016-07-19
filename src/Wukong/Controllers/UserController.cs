using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Wukong.Services;
using Wukong.Models;
using Wukong.Options;
using Wukong.Repositories;

namespace Wukong.Controllers
{
    [Authorize]
    [Route("api/user")]
    public class UserController : Controller
    {
        private IOptions<UserOption> optionAccessor;
        private IUserSongListRepository userSongListRepository;
        private Provider provider;

        public UserController(IOptions<UserOption> optionAccessor,
            IOptions<ProviderOption> providerOption,
            IUserSongListRepository userSongListRepository)
        {
            this.optionAccessor = optionAccessor;
            this.userSongListRepository = userSongListRepository;
            provider = new Provider(providerOption.Value.Url);
        }

        string UserId
        {
            get
            {
                return HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
        }

        [HttpGet("userinfo")]
        public IActionResult GetUserinfo()
        {
            if (!(HttpContext.User.FindFirst(ClaimTypes.Authentication).Value == "true"))
            {
                return new RedirectResult("/access/denied");
            }
            var user = Storage.Instance.GetUser(UserId);
            // TODO(Leeleo3x): In the future we should make request to get more user info.
            user.UpdateFromClaims(HttpContext.User);
            return new ObjectResult(user);
        }

        [HttpPost("songList/{id}")]
        public async Task<IActionResult> SongListAsyc(long id, [FromBody] ClientSongList info)
        {
            // We need to remove duplicate songs in the song list.
            var songList = new HashSet<ClientSong>(info.Song);
            info.Song = new List<ClientSong>(songList);
            if (await userSongListRepository.UpdateAsync(UserId, id, info))
            {
                return new EmptyResult();
            }
            else
            {
                return new NotFoundResult();
            }
        }


        [HttpPost("songList")]
        public async Task<IActionResult> SongListAsyc([FromBody] ClientSongList info)
        {
            // We need to remove duplicate songs in the song list.
            var songList = new HashSet<ClientSong>(info.Song);
            info.Song = new List<ClientSong>(songList);
            var id = await userSongListRepository.AddAsync(UserId, info);
            return new ObjectResult(new CreateSongListResponse
            {
                Id = id
            });
        }

        [HttpGet("songList/{id}")]
        public async Task<IActionResult> SongListAsyc(long id)
        {
            var clientSongList = await userSongListRepository.GetAsync(UserId, id);
            if (clientSongList == null)
            {
                return new NotFoundResult();
            }
            var songFetchTasks = clientSongList?.Song.Select(s => provider.GetSong(s)).ToArray();
            Task.WaitAll(songFetchTasks.ToArray());
            var songs = songFetchTasks.Where(t => t.Result != null).Select(t => t.Result).ToList();
            var songList = new SongList
            {
                Name = clientSongList.Name,
                Song = songs,
            };
            return new ObjectResult(songList);
        }

        [HttpGet("songList")]
        public async Task<IActionResult> SongListAsyc()
        {
            var songList = await userSongListRepository.ListAsync(UserId);
            if (songList == null) return new ObjectResult(new string[0]);
            var result = songList.Select(it => new
            {
                Id = it.Id,
                Name = it.Name
            }).ToList();
            return new ObjectResult(result);
        }
    }

}