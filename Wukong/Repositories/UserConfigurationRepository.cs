using System.Threading.Tasks;

using Wukong.Models;

namespace Wukong.Repositories
{
    public interface IUserConfigurationRespository
    {
        Task<UserConfigurationData> GetAsync(string userId);
        Task AddOrUpdateAsync(string userId, string syncPlaylists, string cookies);
    }

    public class UserConfigurationRepository
    {
        private readonly UserConfigurationContext context;

        public UserConfigurationRepository(UserConfigurationContext context)
        {
            this.context = context;
        }

        public async Task<UserConfigurationData> GetAsync(string userId)
        {
            return await context.UserConfiguration
                .FindAsync(userId) ??
                context.UserConfiguration.Add(new UserConfigurationData
                {
                    UserId = userId
                }).Entity;
        }

        public async Task AddOrUpdateAsync(string userId, string syncPlaylists, string cookies)
        {
            var userConfiguration = await context.UserConfiguration
                .FindAsync(userId) ??
                context.UserConfiguration.Add(new UserConfigurationData
                {
                    UserId = userId
                }).Entity;

            userConfiguration.SyncPlaylists = syncPlaylists;
            userConfiguration.Cookies = cookies;

            await context.SaveChangesAsync();
        }
    }
}
