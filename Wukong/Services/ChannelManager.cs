using Microsoft.Extensions.Logging;
using Wukong.Models;

namespace Wukong.Services
{
    public interface IChannelManager
    {
        void JoinAndLeavePreviousChannel(string channelId, User user);
        void Leave(string userId);
    }
    public class ChannelManager : IChannelManager
    {
        private readonly ILogger logger;
        private readonly IStorage storage;
        private readonly ISocketManager socketManager;
        private readonly IProvider provider;
        private readonly IUserManager userManager;

        public ChannelManager(ILoggerFactory loggerFactory, IStorage storage, ISocketManager socketManager, IProvider provider, IUserManager userManager)
        {
            logger = loggerFactory.CreateLogger<ChannelManager>();
            this.storage = storage;
            this.provider = provider;
            this.socketManager = socketManager;
            this.userManager = userManager;
            this.userManager.UserConnected += UserConnected;
            this.userManager.UserTimeout += UserTimeout;
        }

        public void JoinAndLeavePreviousChannel(string channelId, User user)
        {
            if (channelId == storage.GetChannelByUser(user.Id)?.Id) return;
            Leave(user.Id);
            var channel = storage.GetOrCreateChannel(channelId, socketManager, provider, userManager);
            channel.Join(user.Id);
            user.Join();
        }

        public void Leave(string userId)
        {
            var channel = storage.GetChannelByUser(userId);
            channel?.Leave(userId);
            if (channel == null || !channel.Empty) return;
            logger.LogInformation($"Channel {channel.Id} removed.");
            storage.RemoveChannel(channel.Id);
        }

        private void UserConnected(User user)
        {
            storage.GetChannelByUser(user.Id)?.Connect(user.Id);
        }

        private void UserTimeout(User user)
        {
            Leave(user.Id);
        }
    }
}