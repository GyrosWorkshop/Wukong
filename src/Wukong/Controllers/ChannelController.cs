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

        public ChannelController(IOptions<ProviderOption> providerOption, ILoggerFactory loggerFactory, IChannelServiceFactory channelServiceFactory)
        {
            ChannelServiceFactory = channelServiceFactory;
            Logger = loggerFactory.CreateLogger("ChannelController");
        }

        string UserId
        {
            get
            {
                return HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
        }

        // POST api/channel/join
        [HttpPost("join/{channelId}")]
        public void Join(string channelId, [FromBody] Join join)
        {
            Storage.Instance.GetChannel(join?.PreviousChannelId)?.Leave(UserId);
            ChannelServiceFactory.GetChannel(channelId).Join(UserId);
        }

        [HttpPost("finished/{channelId}")]
        public void Finished(string channelId, [FromBody] ClientSong song)
        {
            // FIXME: test whether user joined this channel.
            Storage.Instance.GetChannel(channelId).ReportFinish(UserId, song);
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
        public void DownVote(string channelId, [FromBody] ClientSong song)
        {
            // FIXME: test whether user joined this channel.
            var channel = Storage.Instance.GetChannel(channelId);
            channel.ReportFinish(UserId, song, true);
        }
    }
}
