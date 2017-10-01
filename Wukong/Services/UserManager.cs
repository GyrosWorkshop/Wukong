using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Wukong.Models;

namespace Wukong.Services
{
    public delegate void UserStateDelegate(User user);

    public interface IUserManager
    {
        User GetAndUpdateUserWithClaims(ClaimsPrincipal claims);
        User GetUser(string userId);
        event UserStateDelegate UserConnected;
        event UserStateDelegate UserDisconnected;
        event UserStateDelegate UserTimeout;
    }

    public interface IUserService
    {
        User User { get; set; }
    }

    public class UserManagerMiddleware
    {
        private readonly RequestDelegate next;
        public UserManagerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task Invoke(HttpContext ctx, IUserManager userManager, IUserService userService)
        {
            var user = userManager.GetAndUpdateUserWithClaims(ctx.User);
            userService.User = user;
            await next(ctx);
        }
    }


    public class UserManager : IUserManager
    {
        private readonly ConcurrentDictionary<string, User> userMap = new ConcurrentDictionary<string, User>();
        private readonly ILoggerFactory loggerFactory;

        public event UserStateDelegate UserConnected;
        public event UserStateDelegate UserDisconnected;
        public event UserStateDelegate UserTimeout;

        public UserManager(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public User GetAndUpdateUserWithClaims(ClaimsPrincipal claims)
        {
            var user = GetOrCreate(claims.FindFirst(ClaimTypes.AuthenticationMethod)?.Value,
                claims.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            user?.UpdateFromClaims(claims);
            if (user == null) return null;

            user.UserConnected -= OnUserConnected;
            user.UserDisconnected -= OnUserDisconnected;
            user.UserTimeout -= OnUserTimeout;
            user.UserConnected += OnUserConnected;
            user.UserDisconnected += OnUserDisconnected;
            user.UserTimeout += OnUserTimeout;
            return user;
        }

        private void OnUserConnected(User user) => UserConnected?.Invoke(user);
        private void OnUserDisconnected(User user) => UserDisconnected?.Invoke(user);
        private void OnUserTimeout(User user) => UserTimeout?.Invoke(user);

        private User GetOrCreate(string fromSite, string siteUserId)
        {
            var id = User.BuildUserIdentifier(fromSite, siteUserId);
            return siteUserId == null ? null : userMap.GetOrAdd(id, _ => new User(fromSite, siteUserId, loggerFactory));
        }

        public User GetUser(string userId)
        {
            return userMap[userId];
        }
    }


    public class UserService : IUserService
    {
        public User User { get; set; }
    }
}
