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

        IDictionary<string, User> userMap = new ConcurrentDictionary<string, User>();
        IDictionary<string, Channel> channelMap = new ConcurrentDictionary<string, Channel>();

        public static Storage Instance
        {
            get
            {
                return instance.Value;
            }
        }

        Storage() { }

        public User GetUser(string userId)
        {
            if (!userMap.ContainsKey(userId))
            {
                userMap.Add(userId, new User(userId));
            }
            return userMap[userId];
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
            channelMap.Remove(channelId);
        }

        public Channel GetChannelByUser(string userId)
        {
            return channelMap.Values.FirstOrDefault(it => it.HasUser(userId));
        }

        public Channel CreateChannel(string channelId, ISocketManager socketManager, IProvider provider)
        {
            channelMap[channelId] = new Channel(channelId, socketManager, provider);
            return channelMap[channelId];
        }
    }

}