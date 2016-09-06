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
        new public string EventName = "NextSongUpdate";
        public string ChannelId;
        public Song Song;
    }

    public class UserListUpdated : WebSocketEvent
    {
        new public string EventName = "UserListUpdated";
        public string ChannelId;
        public IList<User> Users;
    }

    public class Play : WebSocketEvent
    {
        new public string EventName = "Play";
        public string ChannelId;
        public Boolean Downvote;
        public Song Song;
        public double Elapsed;
        public string User;
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
        public string ClientIP;
    }

    public class CreateOrUpdateSongListResponse
    {
        public long Id;
    }

}