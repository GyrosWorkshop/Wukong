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

    public class WebSocketEvent
    {
        public string EventName;
    }

    public class NextSongUpdated : WebSocketEvent
    {
        public new string EventName = "NextSongUpdate";
        public string ChannelId;
        public Song Song;
    }

    public class UserListUpdated : WebSocketEvent
    {
        public new string EventName = "UserListUpdated";
        public string ChannelId;
        public IList<User> Users;
    }

    public class Play : WebSocketEvent
    {
        public new string EventName = "Play";
        public string ChannelId;
        public Boolean Downvote;
        public Song Song;
        public double Elapsed;
        public string User;
    }

    public class Message : WebSocketEvent
    {
        public new string EventName = "Message";
        public string ChannelId;
        public string Sender;
        public string Content;
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