using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

using Wukong.Models;

namespace Wukong.Services
{
    public sealed class Storage
    {
        static readonly Lazy<Storage> instance =
            new Lazy<Storage>(() => new Storage());

        ConcurrentDictionary<string, User> userMap = new ConcurrentDictionary<string, User>();
        ConcurrentDictionary<string, Channel> channelMap = new ConcurrentDictionary<string, Channel>();

        public static Storage Instance => instance.Value;

        private Storage() { }

        public User GetOrCreateUser(string userId)
        {
            return userMap.GetOrAdd(userId, s => new User(s));
        }

        public Channel GetOrCreateChannel(string channelId, ISocketManager socketManager, IProvider provider)
        {
            if (channelId == null) return null;
            return channelMap.GetOrAdd(channelId, s => new Channel(s, socketManager, provider));
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

        public List<Channel> GetAllChannelsWithUserId(string userId)
        {
            return channelMap.Values.Where(x => x.HasUser(userId)).ToList();
        }
    }
}