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
        private readonly IChannelServiceFactory ChannelServiceFactory;
        private readonly IChannelManager ChannelManager;

        public ChannelController(IOptions<ProviderOption> providerOption, ILoggerFactory loggerFactory, IChannelServiceFactory channelServiceFactory, IChannelManager channelManager)
        {
            ChannelServiceFactory = channelServiceFactory;
            Logger = loggerFactory.CreateLogger("ChannelController");
            ChannelManager = channelManager;
        }

        string UserId => HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

        // POST api/channel/join
        [HttpPost("join/{channelId}")]
        public void Join(string channelId, [FromBody] Join join)
        {
            ChannelManager.Leave(channelId, UserId);
            ChannelManager.Join(channelId, UserId);
        }

        [HttpPost("finished/{channelId}")]
        public ActionResult Finished(string channelId, [FromBody] ClientSong song)
        {
            // FIXME: test whether user joined this channel.
            var success = Storage.Instance.GetChannel(channelId).ReportFinish(UserId, song);
            if (success) return NoContent();
            else return BadRequest();
        }

        // POST api/channel/updateNextSong
        [HttpPost("updateNextSong/{channelId}")]
        public void UpdateNextSong(string channelId, [FromBody] ClientSong song)
        {
            // FIXME: test whether user joined this channel.
            var channel = Storage.Instance.GetChannel(channelId);
            channel?.UpdateSong(UserId, song);
        }

        [HttpPost("downVote/{channelId}")]
        public ActionResult DownVote(string channelId, [FromBody] ClientSong song)
        {
            // FIXME: test whether user joined this channel.
            var channel = Storage.Instance.GetChannel(channelId);
            var success = channel.ReportFinish(UserId, song, true);
            if (success) return NoContent();
            else return BadRequest();
        }
    }
}
