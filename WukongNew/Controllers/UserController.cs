using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Wukong.Services;
using Wukong.Models;
using Wukong.Repositories;

namespace Wukong.Controllers
{
    [Authorize]
    [Route("api/user")]
    public class UserController : Controller
    {
        private readonly IProvider _provider;
        private readonly IUserSongListRepository _userSongListRepository;
        private readonly IUserService _userService;

        public UserController(IProvider provider, IUserSongListRepository userSongListRepository, IUserService userService)
        {
            _userSongListRepository = userSongListRepository;
            _provider = provider;
            _userService = userService;
        }

        [HttpGet("userinfo")]
        public IActionResult GetUserinfo()
        {
            return new ObjectResult(_userService.User);
        }

        [HttpPost("songList/{id}")]
        public async Task<IActionResult> SongListAsyc(long id, [FromBody] ClientSongList info)
        {
            // We need to remove duplicate songs in the song list.
            var songList = new HashSet<ClientSong>(info.Song);
            info.Song = new List<ClientSong>(songList);
            if (await _userSongListRepository.UpdateAsync(_userService.User.Id, id, info))
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
            var id = await _userSongListRepository.AddAsync(_userService.User.Id, info);
            return new ObjectResult(new CreateOrUpdateSongListResponse
            {
                Id = id
            });
        }

        [HttpGet("songList/{id}")]
        public async Task<IActionResult> SongListAsyc(long id)
        {
            var clientSongList = await _userSongListRepository.GetAsync(_userService.User.Id, id);
            if (clientSongList == null)
            {
                return new NotFoundResult();
            }
            var songFetchTasks = clientSongList?.Song.Select(s => _provider.GetSong(s)).ToArray();
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
            var songList = await _userSongListRepository.ListAsync(_userService.User.Id);
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