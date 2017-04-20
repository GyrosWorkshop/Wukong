using System.Collections.Concurrent;
using System.Linq;

namespace Wukong.Services
{
    public interface IStorage
    {
        Channel GetOrCreateChannel(string channelId, ISocketManager socketManager, IProvider provider, IUserManager userManager);
        Channel GetChannel(string channelId);
        void RemoveChannel(string channelId);
        Channel GetChannelByUser(string userId);
    }

    public sealed class Storage : IStorage
    {
        
        private readonly ConcurrentDictionary<string, Channel> channelMap = new ConcurrentDictionary<string, Channel>();

        public Channel GetOrCreateChannel(string channelId, ISocketManager socketManager, IProvider provider, IUserManager userManager)
        {
            // todo: move channel creation to ChannelManager
            return channelId == null ? null : channelMap.GetOrAdd(channelId, s => new Channel(channelId, socketManager, provider, userManager));
        }

        public Channel GetChannel(string channelId)
        {
            if (channelId == null || !channelMap.ContainsKey(channelId))
            {
                return null;
            }
            return channelMap[channelId];
        }

        public void RemoveChannel(string channelId)
        {
            Channel ignore;
            channelMap.TryRemove(channelId, out ignore);
        }

        public Channel GetChannelByUser(string userId)
        {
            return channelMap.Values.FirstOrDefault(it => it.HasUser(userId));
        }
    }
}