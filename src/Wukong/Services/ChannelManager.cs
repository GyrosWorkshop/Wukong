using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wukong.Models;

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
        private readonly IUserManager UserManager;

        public ChannelManager(ILoggerFactory loggerFactory, IStorage storage, ISocketManager socketManager, IProvider provider, IUserManager userManager)
        {
            Logger = loggerFactory.CreateLogger<ChannelManager>();
            Storage = storage;
            Provider = provider;
            SocketManager = socketManager;
            UserManager = userManager;
            UserManager.UserConnected += UserConnected;
            UserManager.UserTimeout += UserTimeout;
        }

        public void JoinAndLeavePreviousChannel(string channelId, string userId)
        {
            if (channelId == Storage.GetChannelByUser(userId)?.Id) return;
            Leave(userId);
            var channel = Storage.GetOrCreateChannel(channelId, SocketManager, Provider, UserManager);
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

        private void UserConnected(User user)
        {
            Storage.GetChannelByUser(user.Id)?.Connect(user.Id);
        }

        private void UserTimeout(User user)
        {
            Leave(user.Id);
        }
    }
}