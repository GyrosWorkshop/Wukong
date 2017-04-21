using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Wukong.Models;

namespace Wukong.Repositories
{
    public interface IUserConfigurationRepository
    {
        Task<UserConfigurationData> GetAsync(string siteId, string userId);
        Task AddOrUpdateAsync(string siteId, string userId, string syncPlaylists, string cookies);
    }

    public class UserConfigurationRepository : IUserConfigurationRepository
    {
        private readonly CloudTable table;

        public UserConfigurationRepository(CloudStorageAccount account)
        {
            var client = account.CreateCloudTableClient();
            table = client.GetTableReference("userconfiguration");
            table.CreateIfNotExistsAsync().Wait();
        }

        public async Task<UserConfigurationData> GetAsync(string siteId, string userId)
        {
            var operation = TableOperation.Retrieve<UserConfigurationData>(siteId, userId);
            var result = await table.ExecuteAsync(operation);
            return (UserConfigurationData) result.Result;
        }

        public async Task AddOrUpdateAsync(string siteId, string userId, string syncPlaylists, string cookies)
        {
            var data = new UserConfigurationData
            {
                SyncPlaylists = syncPlaylists,
                PartitionKey = siteId,
                RowKey = userId,
                Cookies = cookies
            };

            var operation = TableOperation.InsertOrReplace(data);
            await table.ExecuteAsync(operation);
        }
    }
}