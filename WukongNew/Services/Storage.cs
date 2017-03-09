using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

using Wukong.Models;

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
        
        private readonly ConcurrentDictionary<string, Channel> ChannelMap = new ConcurrentDictionary<string, Channel>();

        public Channel GetOrCreateChannel(string channelId, ISocketManager socketManager, IProvider provider, IUserManager userManager)
        {
            // todo: move channel creation to ChannelManager
            return channelId == null ? null : ChannelMap.GetOrAdd(channelId, s => new Channel(s, socketManager, provider, this, userManager));
        }

        public Channel GetChannel(string channelId)
        {
            if (channelId == null || !ChannelMap.ContainsKey(channelId))
            {
                return null;
            }
            return ChannelMap[channelId];
        }

        public void RemoveChannel(string channelId)
        {
            Channel ignore;
            ChannelMap.TryRemove(channelId, out ignore);
        }

        public Channel GetChannelByUser(string userId)
        {
            return ChannelMap.Values.FirstOrDefault(it => it.HasUser(userId));
        }
    }
}