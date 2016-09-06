using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Wukong.Services
{
    public interface IChannelManager
    {
        void JoinAndLeavePreviousChannel(string channelId, string userId);
        void Leave(string userId);
        ISocketManager SocketManager { set; }
    }
    public class ChannelManager : IChannelManager
    {
        private readonly ILogger Logger;
        private readonly IStorage Storage;

        public ISocketManager SocketManager { get; set; }

        public ChannelManager(ILoggerFactory loggerFactory, IStorage storage)
        {
            Logger = loggerFactory.CreateLogger<ChannelManager>();
            Storage = storage;
        }
        public void JoinAndLeavePreviousChannel(string channelId, string userId)
        {
            if (channelId == Storage.GetChannelByUser(userId)?.Id) return;
            Leave(userId);
            var channel = Storage.GetOrCreateChannel(channelId);
            channel.Join(userId);
        }

        public void BroadCastUserList(Channel channel, string userId = null)
        {
            var userList = userId != null ? new List<string> { userId } : channel.UserList;
        }

        public void Leave(string userId)
        {
            var channel = Storage.GetChannelByUser(userId);
            if (channel != null)
            {
                channel.Leave(userId);
                if (channel.Empty)
                {
                    Logger.LogInformation($"Channel {channel.Id} removed.");
                    Storage.RemoveChannel(channel.Id);
                }
            }
        }

    }
}