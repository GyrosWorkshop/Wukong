using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Security.Claims;
using Wukong.Services;
using Wukong.Utilities;
using Wukong.Models;
using Wukong.Options;
using Microsoft.Extensions.Logging;

namespace Wukong.Controllers
{


    [Authorize]
    [Route("api/[controller]")]
    public class ChannelController : Controller
    {
        private readonly Provider provider;
        private readonly ILogger Logger;
        private readonly ISocketManager SocketManager;
        private static Mutex mut = new Mutex();
        private static bool DidInitializeSocketManager = false;
        private IDictionary<string, AsyncManualResetEvent> startPlayingEvents = new Dictionary<string, AsyncManualResetEvent>();

        public ChannelController(IOptions<ProviderOption> providerOption, ILoggerFactory loggerFactory, ISocketManager socketManager)
        {
            this.provider = new Provider(providerOption.Value.Url);
            Logger = loggerFactory.CreateLogger("ChannelController");
            SocketManager = socketManager;
            mut.WaitOne();
            if (!DidInitializeSocketManager)
            {
                DidInitializeSocketManager = true;
                SocketManager.UserDisconnect += UserDisconnect;
                SocketManager.UserConnect += UserConnect;
            }
            mut.ReleaseMutex();
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
        public IActionResult Join(string channelId, [FromBody] Join join)
        {
            var previousChannel = Storage.Instance.GetChannel(join?.PreviousChannelId);
            previousChannel?.Leave(UserId);

            var channel = Storage.Instance.GetChannel(channelId) ?? CreateChannel(channelId);
            channel.Join(UserId);
            EmitChannelInfo(channel, UserId);

            // Hmmm, may be we need a better way.
            return new EmptyResult();
        }

        [HttpPost("finished/{channelId}")]
        public IActionResult Finished(string channelId)
        {
            var channel = Storage.Instance.GetChannel(channelId);
            channel.AddFinishedUser(UserId);
            return new EmptyResult();
        }

        // POST api/channel/updateNextSong
        [HttpPost("updateNextSong/{channelId}")]
        public IActionResult UpdateNextSong(string channelId, [FromBody] ClientSong song)
        {
            var channel = Storage.Instance.GetChannel(channelId);
            channel?.UpdateSong(UserId, song);
            return new EmptyResult();
        }

        [HttpPost("downVote/{channelId}")]
        public IActionResult DownVote(string channelId)
        {
            var channel = Storage.Instance.GetChannel(channelId);
            channel.DownVote(UserId);
            return new EmptyResult();
        }

        private async void StartPlaying(Channel channel)
        {
            channel.StartPlayingNextSong();
            var song = await provider.GetSong(channel.CurrentSong, true);
            if (song != null)
            {
                StartMonitor(channel, song);
            }
            channel.StartTime = DateTime.Now;
            SocketManager.SendMessage(channel.UserList, new Play
            {
                Song = song,
                Elapsed = 0,
                User = channel.CurrentUserId,
            });
        }

        private Channel CreateChannel(string channelId)
        {
            var channel = Storage.Instance.CreateChannel(channelId);
            channel.ShouldForwardCurrentSong += ShouldForwardCurrentSong;
            channel.NextSongUpdated += NextSongUpdated;
            channel.UserListUpdated += UserListUpdated;
            return channel;
        }

        private void ShouldForwardCurrentSong(string channelId)
        {
            startPlayingEvents[channelId]?.Set();
        }

        private async void NextSongUpdated(string channelId)
        {
            var channel = Storage.Instance.GetChannel(channelId);
            if (channel.CurrentSong == null)
            {
                if (startPlayingEvents.ContainsKey(channelId))
                {
                    startPlayingEvents[channelId].Set();
                }
                else 
                {
                    StartPlaying(channel);
                }
            }
            else
            {
                SocketManager.SendMessage(channel.UserList, new Wukong.Models.NextSongUpdated
                {
                    Song = await provider.GetSong(channel.NextSong, true),
                });
            }
        }

        private void UserListUpdated(string channelId)
        {
            var channel = Storage.Instance.GetChannel(channelId);
            SendUserListUpdatedEvent(channel.UserList, channel.UserList);
        }

        private void SendUserListUpdatedEvent(List<string> recipients, List<string> users)
        {
            var objects = users.Select(i => Storage.Instance.GetUser(i)).ToList();

            SocketManager.SendMessage(recipients, new Wukong.Models.UserListUpdated
            {
                Users = objects
            });
        }

        async private void StartMonitor(Channel channel, Song song)
        {
            Logger.LogDebug("StartMonitor", DateTime.Now, song);
            startPlayingEvents[channel.Id] = new AsyncManualResetEvent(false);
            var checker = new TimerChecker(10, startPlayingEvents[channel.Id]);
            var timer = new Timer(checker.Check, channel, (int)song.Length, 1000);

            await startPlayingEvents[channel.Id].WaitAsync();
            Logger.LogDebug("StartPlaying", DateTime.Now, song);

            startPlayingEvents.Remove(channel.Id);
            timer.Dispose();
            StartPlaying(channel);
        }

        private void UserDisconnect(string userId)
        {
            var channels = Storage.Instance.GetAllChannelsWithUserId(userId);
            channels.ForEach(x => x.Leave(userId));
        }

        private void LeaveChannel(string userId, Channel channel)
        {
            channel?.Leave(userId);
            if (channel?.UserList.Count == 0)
            {
                Storage.Instance.RemoveChannel(channel.Id);
            }
        }

        private void UserConnect(string userId)
        {
            var channels = Storage.Instance.GetAllChannelsWithUserId(userId);
            channels.ForEach(channel => EmitChannelInfo(channel, userId));
        }

        private async void EmitChannelInfo(Channel channel, string userId)
        {
            SocketManager.SendMessage(new List<string> { userId }, new Play
            {
                Elapsed = channel.Elapsed,
                User = channel.CurrentUserId,
                Song = await provider.GetSong(channel.CurrentSong, true),
            });
            SendUserListUpdatedEvent(new List<string> { userId }, channel.UserList);
        }
    }
}
