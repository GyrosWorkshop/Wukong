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
    }
    public class ChannelManager : IChannelManager
    {
        private readonly ILogger Logger;
        private readonly IStorage Storage;
        private readonly ISocketManager SocketManager;
        private readonly IProvider Provider;

        public ChannelManager(ILoggerFactory loggerFactory, IStorage storage, ISocketManager socketManager, IProvider provider)
        {
            Logger = loggerFactory.CreateLogger<ChannelManager>();
            Storage = storage;
            Provider = provider;
            SocketManager = socketManager;
            SocketManager.ConnectedEvent += UserConnected;
            SocketManager.DisconnectedEvent += UserDisconnected;
        }

        public void JoinAndLeavePreviousChannel(string channelId, string userId)
        {
            if (channelId == Storage.GetChannelByUser(userId)?.Id) return;
            Leave(userId);
            var channel = Storage.GetOrCreateChannel(channelId, SocketManager, Provider);
            channel.Join(userId);
        }

        public void BroadCastUserList(Channel channel, string userId = null)
        {
            var userList = userId != null ? new List<string> { userId } : channel.UserList;
        }

        public void Leave(string userId)
        {
            var channel = Storage.GetChannelByUser(userId);
            channel?.Leave(userId);
            if (channel == null || !channel.Empty) return;
            Logger.LogInformation($"Channel {channel.Id} removed.");
            Storage.RemoveChannel(channel.Id);
        }

        private void UserConnected(string userId)
        {
            Storage.GetChannelByUser(userId)?.Connect(userId);
        }

        private void UserDisconnected(string userId)
        {
            Leave(userId);
        }
    }
}