using System;
using Wukong.Models;
using Wukong.Services;
using Xunit;

namespace Wukong.Tests
{
    public class ChannelUserListTest
    {
        private const string UserId1 = "User1";
        private const string UserId2 = "User2";
        private const string UserId3 = "User3";

        private static readonly ClientSong ClientSong1 = new ClientSong()
        {
            SiteId = "",
            SongId = "Song1"
        };

        private static readonly ClientSong ClientSong2 = new ClientSong()
        {
            SiteId = "",
            SongId = "Song2"
        };

        private static readonly ClientSong ClientSong3 = new ClientSong()
        {
            SiteId = "",
            SongId = "Song3"
        };

        [Fact]
        public void SingleUser()
        {
            var list = new ChannelUserList();
            var userAdd = false;
            UserSong current = null;
            var userChangedCalled = 0;
            var currentChangedCalled = 0;

            list.UserChanged += (add, id) =>
            {
                Assert.Equal(id, UserId1);
                userAdd = add;
                userChangedCalled++;
            };

            list.CurrentChanged += song =>
            {
                current = song;
                currentChangedCalled++;
            };

            Assert.True(list.Empty);
            Assert.False(list.IsPlaying);

            list.AddUser(UserId1);
            Assert.False(list.Empty);
            Assert.False(list.IsPlaying);
            Assert.True(userAdd);
            Assert.Equal(userChangedCalled, 1);
            Assert.Equal(list.UserList.Count, 1);

            list.SetSong(UserId1, ClientSong1);
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 1);
            Assert.Equal(list.CurrentPlaying, current);
            Assert.Equal(current.Song, ClientSong1);
            Assert.Equal(current.UserId, UserId1);

            list.SetSong(UserId1, ClientSong2);
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 1);
            Assert.Equal(list.CurrentPlaying, current);
            Assert.Equal(current.Song, ClientSong1);
            Assert.Equal(current.UserId, UserId1);

            list.GoNext();
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 2);
            Assert.Equal(list.CurrentPlaying, current);
            Assert.Equal(current.Song, ClientSong2);
            Assert.Equal(current.UserId, UserId1);

            list.RemoveUser(UserId1);
            Assert.True(list.Empty);
            Assert.False(list.IsPlaying);
            Assert.Equal(userChangedCalled, 2);
            Assert.Equal(currentChangedCalled, 3);
            Assert.Equal(userAdd, false);
            Assert.Equal(list.CurrentPlaying, current);
            Assert.Equal(current, null);
            Assert.Equal(list.UserList.Count, 0);
        }

        [Fact]
        public void MultipleUser()
        {
            var list = new ChannelUserList();
            var userAdd = false;
            var userId = "";
            UserSong current = null;
            var userChangedCalled = 0;
            var currentChangedCalled = 0;

            list.UserChanged += (add, id) =>
            {
                userAdd = add;
                userId = id;
                userChangedCalled++;
            };

            list.CurrentChanged += song =>
            {
                current = song;
                currentChangedCalled++;
            };

            Assert.True(list.Empty);
            Assert.False(list.IsPlaying);

            list.AddUser(UserId1);
            Assert.False(list.Empty);
            Assert.False(list.IsPlaying);
            Assert.True(userAdd);
            Assert.Equal(userChangedCalled, 1);
            Assert.Equal(list.UserList.Count, 1);

            list.AddUser(UserId2);
            Assert.False(list.Empty);
            Assert.False(list.IsPlaying);
            Assert.True(userAdd);
            Assert.Equal(userChangedCalled, 2);
            Assert.Equal(list.UserList.Count, 2);

            list.AddUser(UserId3);
            Assert.False(list.Empty);
            Assert.False(list.IsPlaying);
            Assert.True(userAdd);
            Assert.Equal(userChangedCalled, 3);
            Assert.Equal(list.UserList.Count, 3);

            Assert.Equal(currentChangedCalled, 0);

            // No user setup there songs, so we should not play anything.
            list.GoNext();
            list.GoNext();
            Assert.Equal(userChangedCalled, 3);
            Assert.Equal(currentChangedCalled, 0);

            // The last user setup a song, auto start playing his/her song.
            list.SetSong(UserId3, ClientSong3);
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(currentChangedCalled, 1);
            Assert.Equal(current.UserId, UserId3);
            Assert.Equal(current.Song, ClientSong3);

            // Cause only one user set his/her song, we should always play this song.
            list.GoNext();
            list.GoNext();
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(currentChangedCalled, 3);
            Assert.Equal(current.UserId, UserId3);
            Assert.Equal(current.Song, ClientSong3);

            // Current user changes his/her song, we should not forward until current song is finished.
            list.SetSong(UserId3, ClientSong1);
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(currentChangedCalled, 3);
            Assert.Equal(current.UserId, UserId3);
            Assert.Equal(current.Song, ClientSong3);

            // Current song finished, we should play next song of current user.
            list.GoNext();
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(currentChangedCalled, 4);
            Assert.Equal(current.UserId, UserId3);
            Assert.Equal(current.Song, ClientSong1);

            // We set this for convenience of test.
            list.SetSong(UserId3, ClientSong3);

            // The others setup their next song, we should not play until current finished.
            list.SetSong(UserId2, ClientSong2);
            list.SetSong(UserId1, ClientSong1);
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 4);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(current.UserId, UserId3);
            Assert.Equal(current.Song, ClientSong1);

            list.GoNext();
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 5);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(current.UserId, UserId1);
            Assert.Equal(current.Song, ClientSong1);

            list.GoNext();
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 6);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(current.UserId, UserId2);
            Assert.Equal(current.Song, ClientSong2);

            list.GoNext();
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 7);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(current.UserId, UserId3);
            Assert.Equal(current.Song, ClientSong3);

            list.GoNext();
            list.GoNext();
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 9);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(current.UserId, UserId2);
            Assert.Equal(current.Song, ClientSong2);

            // Current user (the second) exit, we should continue playing his/her song cause there are still some users in this channel.
            list.RemoveUser(UserId2);
            Assert.False(list.Empty);
            Assert.True(list.IsPlaying);
            Assert.Equal(currentChangedCalled, 9);
            Assert.Equal(current, list.CurrentPlaying);
            Assert.Equal(current.UserId, UserId2);
            Assert.Equal(current.Song, ClientSong2);

            // After the song of exit user finished, play next user's song.
            // TODO: we chould not make it happen now.
//            list.GoNext();
//            Assert.False(list.Empty);
//            Assert.True(list.IsPlaying);
//            Assert.Equal(currentChangedCalled, 10);
//            Assert.Equal(current, list.CurrentPlaying);
//            Assert.Equal(current.UserId, UserId3);
//            Assert.Equal(current.Song, ClientSong3);
        }
    }
}
