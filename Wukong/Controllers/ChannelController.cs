using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Wukong.Services;
using Wukong.Models;

namespace Wukong.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ChannelController : Controller
    {
        private readonly IChannelManager channelManager;
        private readonly IStorage storage;
        private readonly IUserService userService;
        private readonly ISongStorage songStorage;

        public ChannelController(IChannelManager channelManager, IStorage storage, 
            IUserService userService, ISongStorage songStorage)
        {
            this.channelManager = channelManager;
            this.storage = storage;
            this.userService = userService;
            this.songStorage = songStorage;
        }

        // POST api/channel/join
        [HttpPost("join/{channelId}")]
        public ActionResult Join(string channelId)
        {
            channelManager.JoinAndLeavePreviousChannel(channelId, userService.User);
            return NoContent();
        }

        [HttpPost("finished")]
        public ActionResult Finished([FromBody] ClientSong song)
        {
            var success = storage.GetChannelByUser(userService.User.Id)?.ReportFinish(userService.User.Id, song);
            if (success == true) return NoContent();
            return BadRequest();
        }

        [HttpPost("updateNextSong")]
        public ActionResult UpdateNextSong([FromBody] ClientSong song)
        {
            if (song.IsEmpty()) song = null;
            songStorage.SetNextSong(userService.User, song);
            return NoContent();
        }

        [HttpPost("downVote")]
        public ActionResult DownVote([FromBody] ClientSong song)
        {
            // FIXME: test whether user joined this channel.
            var channel = storage.GetChannelByUser(userService.User.Id);
            var success = channel?.ReportFinish(userService.User.Id, song, true);
            if (success == true) return NoContent();
            return BadRequest();
        }
    }
}