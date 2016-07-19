using Xunit;
using Moq;
using Wukong.Services;

using System.Collections.Generic;

namespace WukongTest
{
    public class ChannelTest
    {
        [Fact]
        void TestJoin()
        {
            var nextSongDelegate = new Mock<NextSongUpdated>(MockBehavior.Strict);
            var channel = new Channel("1");
            channel.NextSongUpdated += nextSongDelegate.Object;
            channel.Join("a");
            Assert.Null(channel.CurrentSong);
            Assert.Null(channel.NextSong);
            Assert.True(channel.IsIdle);
        }

    }
}