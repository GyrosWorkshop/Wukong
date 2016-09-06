using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Wukong.Services
{
    public interface IChannelManager
    {
        void Join(string channelId, string userId);
        void Leave(string channelId, string userId);
        ISocketManager SocketManager { set; }
    }
    public class ChannelManager : IChannelManager
    {
        private IProvider provider;
        private readonly ILogger Logger;

        public ISocketManager SocketManager { get; set; }

        public ChannelManager(IProvider provider, ILoggerFactory loggerFactory)
        {
            this.provider = provider;
            Logger = loggerFactory.CreateLogger<ChannelManager>();
        }
        public void Join(string channelId, string userId)
        {
            var channel = Storage.Instance.GetChannel(channelId) ??
                Storage.Instance.CreateChannel(channelId, SocketManager, provider);
            channel.Join(userId);
        }

        public void BroadCastUserList(Channel channel, string userId = null)
        {
            var userList = userId != null ? new List<string> { userId } : channel.UserList;
        }

        public void Leave(string channelId, string userId)
        {
            var channel = Storage.Instance.GetChannel(channelId);
            if (channel != null)
            {
                channel.Leave(userId);
                if (channel.Empty)
                {
                    Logger.LogInformation($"Channel {channel.Id} removed.");
                    Storage.Instance.RemoveChannel(channelId);
                }
            }
        }

    }
}