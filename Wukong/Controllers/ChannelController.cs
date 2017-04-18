using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Wukong.Services;
using Wukong.Models;
using Wukong.Options;
using Microsoft.Extensions.Logging;

namespace Wukong.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ChannelController : Controller
    {
        private readonly IChannelManager _channelManager;
        private readonly IStorage _storage;
        private readonly IUserService _userService;

        public ChannelController(IChannelManager channelManager, IStorage storage, IUserService userService)
        {
            _channelManager = channelManager;
            _storage = storage;
            _userService = userService;
        }

        // POST api/channel/join
        [HttpPost("join/{channelId}")]
        public ActionResult Join(string channelId)
        {
            _channelManager.JoinAndLeavePreviousChannel(channelId, _userService.User);
            return NoContent();
        }

        [HttpPost("finished")]
        public ActionResult Finished([FromBody] ClientSong song)
        {
            var success = _storage.GetChannelByUser(_userService.User.Id)?.ReportFinish(_userService.User.Id, song);
            if (success == true) return NoContent();
            return BadRequest();
        }

        [HttpPost("updateNextSong")]
        public ActionResult UpdateNextSong([FromBody] ClientSong song)
        {
            if (song.IsEmpty()) song = null;
            var channel = _storage.GetChannelByUser(_userService.User.Id);
            channel?.UpdateSong(_userService.User.Id, song);
            return NoContent();
        }

        [HttpPost("downVote")]
        public ActionResult DownVote([FromBody] ClientSong song)
        {
            // FIXME: test whether user joined this channel.
            var channel = _storage.GetChannelByUser(_userService.User.Id);
            var success = channel?.ReportFinish(_userService.User.Id, song, true);
            if (success == true) return NoContent();
            return BadRequest();
        }
    }
}
