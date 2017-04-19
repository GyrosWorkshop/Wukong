using System;
using System.Collections.Generic;

namespace Wukong.Models
{
    public class TokenResponse
    {
        public string UserId;
        public string Signature;
        public User User;
    }

    public class OAuthMethod
    {
        public string Scheme;
        public string DisplayName;
        public string Url;
    }

    public class WebSocketEvent
    {
        public string EventName;
        public string ChannelId;
    }

    public class NextSongUpdated : WebSocketEvent
    {
        new public string EventName = "Preload";
        public Song Song;
    }

    public class UserListUpdated : WebSocketEvent
    {
        new public string EventName = "UserListUpdated";
        public IList<User> Users;
    }

    public class Play : WebSocketEvent
    {
        new public string EventName = "Play";
        public Boolean Downvote;
        public Song Song;
        public double Elapsed;
        public string User;
    }

    public class Notification
    {
        public string Message;
        public int Timeout;
    }

    public class NotificationEvent : WebSocketEvent
    {
        new public string EventName = "Notification";
        public Notification Notification;
    }

    public class ClientSongList
    {
        public string Name;
        public List<ClientSong> Song;
    }

    public class SongList
    {
        public string Name;
        public List<Song> Song;
    }

    public class SearchSongRequest
    {
        public string Key;
    }

    public class GetSongRequest : ClientSong
    {
        public bool WithFileUrl;
    }

    public class CreateOrUpdateSongListResponse
    {
        public long Id;
    }

}