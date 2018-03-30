using Wukong.Models;

namespace Wukong.Models
{
    public class UserSong
    {
        public delegate void ClientSongChangedHandler(UserSong userSong, ClientSong previousSong);
        public event ClientSongChangedHandler ClientSongChanged;

        public readonly string UserId;
        private ClientSong song;

        public ClientSong Song
        {
            get => song;
            set
            {
                var previous = song;
                song = value;
                OnSongChanged(previous);
            }
        }

        public UserSong(string userId)
        {
            UserId = userId;
        }

        private void OnSongChanged(ClientSong previous)
        {
            ClientSongChanged?.Invoke(this, previous);
        }

        public UserSong Clone()
        {
            return new UserSong(UserId)
            {
                Song = Song
            };
        }
    }
}