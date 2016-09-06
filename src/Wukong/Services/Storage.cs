using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

using Wukong.Models;

namespace Wukong.Services
{
    public interface IStorage
    {
        User GetOrCreateUser(string userId);
        Channel GetOrCreateChannel(string channelId);
        Channel GetChannel(string channelId);
        void RemoveChannel(string channelId);
        Channel GetChannelByUser(string userId);

        ISocketManager SocketManager { set; }
    }

    public sealed class Storage : IStorage
    {
        private readonly IProvider Provider;
        public ISocketManager SocketManager { get; set; }

        private readonly ConcurrentDictionary<string, User> UserMap = new ConcurrentDictionary<string, User>();
        private readonly ConcurrentDictionary<string, Channel> ChannelMap = new ConcurrentDictionary<string, Channel>();

        public Storage(IProvider provider)
        {
            Provider = provider;
        }

        public User GetOrCreateUser(string userId)
        {
            return UserMap.GetOrAdd(userId, s => new User(s));
        }

        public Channel GetOrCreateChannel(string channelId)
        {
            return channelId == null ? null : ChannelMap.GetOrAdd(channelId, s => new Channel(s, SocketManager, Provider, this));
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