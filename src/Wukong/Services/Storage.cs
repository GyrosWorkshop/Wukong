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
        Settings GetSettings(string userId);
        void SaveSettings(string userId, Settings settings);
        Channel GetOrCreateChannel(string channelId, ISocketManager socketManager, IProvider provider);
        Channel GetChannel(string channelId);
        void RemoveChannel(string channelId);
        Channel GetChannelByUser(string userId);
    }

    public sealed class Storage : IStorage
    {
        private readonly ConcurrentDictionary<string, User> UserMap = new ConcurrentDictionary<string, User>();
        private readonly ConcurrentDictionary<string, Settings> UserSettingsMap = new ConcurrentDictionary<string, Settings>();
        private readonly ConcurrentDictionary<string, Channel> ChannelMap = new ConcurrentDictionary<string, Channel>();

        public User GetOrCreateUser(string userId)
        {
            return UserMap.GetOrAdd(userId, s => new User(s));
        }

        public Settings GetSettings(string userId)
        {
            return UserSettingsMap.GetOrAdd(userId, s => new Settings());
        }

        public void SaveSettings(string userId, Settings settings)
        {
            UserSettingsMap.AddOrUpdate(userId, settings, (k, v) =>
            {
                if (settings.UseCdn != null) v.UseCdn = settings.UseCdn;
                return v;
            });
        }

        public Channel GetOrCreateChannel(string channelId, ISocketManager socketManager, IProvider provider)
        {
            return channelId == null ? null : ChannelMap.GetOrAdd(channelId, s => new Channel(s, socketManager, provider, this));
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