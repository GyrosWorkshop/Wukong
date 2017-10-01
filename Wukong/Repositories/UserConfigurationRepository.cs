using System.Threading.Tasks;

using Wukong.Models;

namespace Wukong.Repositories
{
    public interface IUserConfigurationRepository
    {
        Task<UserConfigurationData> GetAsync(string fromSite, string userId);
        Task AddOrUpdateAsync(string fromSite, string userId, string syncPlaylists, string cookies);
    }

    public class UserConfigurationRepository: IUserConfigurationRepository
    {
        private readonly UserDbContext context;

        public UserConfigurationRepository(UserDbContext context)
        {
            this.context = context;
        }

        public async Task<UserConfigurationData> GetAsync(string fromSite, string userId)
        {
            return await context.UserConfiguration
                .FindAsync(fromSite, userId) ??
                context.UserConfiguration.Add(new UserConfigurationData
                {
                    UserId = userId
                }).Entity;
        }

        public async Task AddOrUpdateAsync(string fromSite, string userId, string syncPlaylists, string cookies)
        {
            var userConfiguration = await context.UserConfiguration
                .FindAsync(fromSite, userId) ??
                context.UserConfiguration.Add(new UserConfigurationData
                {
                    FromSite = fromSite,
                    UserId = userId
                }).Entity;

            userConfiguration.SyncPlaylists = syncPlaylists;
            userConfiguration.Cookies = cookies;

            await context.SaveChangesAsync();
        }
    }
}
