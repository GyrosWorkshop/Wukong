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
        private readonly ILogger Logger;
        private readonly IChannelManager ChannelManager;
        private readonly IStorage Storage;
        private readonly IUserService UserService;

        public ChannelController(IOptions<ProviderOption> providerOption, ILoggerFactory loggerFactory, IChannelManager channelManager, IStorage storage, IUserService userService)
        {
            Logger = loggerFactory.CreateLogger("ChannelController");
            ChannelManager = channelManager;
            Storage = storage;
            UserService = userService;
        }

        string UserId => HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

        // POST api/channel/join
        [HttpPost("join/{channelId}")]
        public ActionResult Join(string channelId)
        {
            ChannelManager.JoinAndLeavePreviousChannel(channelId, UserId);
            return NoContent();
        }

        [HttpPost("finished/{channelId}")]
        public ActionResult Finished(string channelId, [FromBody] ClientSong song)
        {
            var success = Storage.GetChannelByUser(UserService.User.Id)?.ReportFinish(UserId, song);
            if (success == true) return NoContent();
            return BadRequest();
        }

        [HttpPost("updateNextSong/{channelId}")]
        public ActionResult UpdateNextSong(string channelId, [FromBody] ClientSong song)
        {
            if (song.IsEmpty()) song = null;
            var channel = Storage.GetChannelByUser(UserService.User.Id);
            channel?.UpdateSong(UserId, song);
            return NoContent();
        }

        [HttpPost("downVote/{channelId}")]
        public ActionResult DownVote(string channelId, [FromBody] ClientSong song)
        {
            // FIXME: test whether user joined this channel.
            var channel = Storage.GetChannelByUser(UserService.User.Id);
            var success = channel?.ReportFinish(UserId, song, true);
            if (success == true) return NoContent();
            return BadRequest();
        }
    }
}
