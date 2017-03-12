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
        private readonly IProvider Provider;
        private readonly IUserSongListRepository UserSongListRepository;
        private readonly IStorage Storage;
        private readonly IUserService UserService;

        public UserController(IProvider provider, IUserSongListRepository userSongListRepository, IStorage storage, IUserService userService)
        {
            UserSongListRepository = userSongListRepository;
            Storage = storage;
            Provider = provider;
            UserService = userService;
        }

        string UserId => HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

        [HttpGet("userinfo")]
        public IActionResult GetUserinfo()
        {
            return new ObjectResult(UserService.User);
        }

        [HttpPost("songList/{id}")]
        public async Task<IActionResult> SongListAsyc(long id, [FromBody] ClientSongList info)
        {
            // We need to remove duplicate songs in the song list.
            var songList = new HashSet<ClientSong>(info.Song);
            info.Song = new List<ClientSong>(songList);
            if (await UserSongListRepository.UpdateAsync(UserId, id, info))
            {
                return new ObjectResult(new CreateOrUpdateSongListResponse
                {
                    Id = id
                });
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
            var id = await UserSongListRepository.AddAsync(UserId, info);
            return new ObjectResult(new CreateOrUpdateSongListResponse
            {
                Id = id
            });
        }

        [HttpGet("songList/{id}")]
        public async Task<IActionResult> SongListAsyc(long id)
        {
            var clientSongList = await UserSongListRepository.GetAsync(UserId, id);
            if (clientSongList == null)
            {
                return new NotFoundResult();
            }
            var songFetchTasks = clientSongList?.Song.Select(s => Provider.GetSong(s)).ToArray();
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
            var songList = await UserSongListRepository.ListAsync(UserId);
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