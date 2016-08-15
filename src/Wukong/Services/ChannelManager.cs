using System.Collections.Generic;

namespace Wukong.Services
{
//    interface IChannelManager
//    {
//        
//    }
//    public class ChannelManager : IChannelManager
//    {
//        private ISocketManager socketManager;
//        private IProvider provider;
//        public ChannelManager(ISocketManager socketManager, IProvider provider)
//        {
//            this.socketManager = socketManager;
//            this.provider = provider;
//        }
//        public void Join(string channelId, string userId)
//        {
//            var channel = Storage.Instance.GetChannel(channelId) ?? 
//                Storage.Instance.CreateChannel(channelId, socketManager, provider);
//            if (channel.UserList.Contains(userId)) {
//            }
//       }
//
//       public void BroadCastUserList(Channel channel, string userId = null)
//       {
//           var userList = userId != null ? new List<string> { userId } : channel.UserList;
//       }
//    }
}