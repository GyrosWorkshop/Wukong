using System;
using Wukong.Models;
using Wukong.Services;
using Xunit;

namespace Wukong.Tests
{
    public class ChannelUserListTest
    {
        private const string UserId1 = "User1";

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
    }
}
