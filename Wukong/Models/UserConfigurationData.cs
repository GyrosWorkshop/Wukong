using Microsoft.WindowsAzure.Storage.Table;

namespace Wukong.Models
{
    public class UserConfigurationData : TableEntity
    {
        public string SyncPlaylists { get; set; }
        public string Cookies { get; set; }
    }
}