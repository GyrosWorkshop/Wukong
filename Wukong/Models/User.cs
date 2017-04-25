using Stateless;
using System.Security.Claims;
using System.Threading;
using Microsoft.Extensions.Logging;
using Wukong.Helpers;
using Wukong.Services;
using static System.Threading.Timeout;

namespace Wukong.Models
{

    public class User
    {
        public static readonly string AvatarKey = "avatar";

        public string UserName { get; private set; }

        private string Email { get; set; }

        public string Id { get; }

        public string Avatar => $"https://secure.gravatar.com/avatar/{Email.MD5String()}";

        private readonly StateMachine<UserState, UserTrigger> userStateMachine =
            new StateMachine<UserState, UserTrigger>(UserState.Created);

        private Timer disconnectTimer;

        private readonly ILogger logger;

        public event UserStateDelegate UserConnected;
        public event UserStateDelegate UserDisconnected;
        public event UserStateDelegate UserTimeout;

        private User()
        {
            userStateMachine.Configure(UserState.Created)
                .Permit(UserTrigger.Connect, UserState.Connected)
                .Permit(UserTrigger.Join, UserState.Joined)
                .Ignore(UserTrigger.Timeout)
                .Ignore(UserTrigger.Disconnect);

            userStateMachine.Configure(UserState.Connected)
                .Permit(UserTrigger.Join, UserState.Playing)
                .Permit(UserTrigger.Disconnect, UserState.Created)
                .Permit(UserTrigger.Timeout, UserState.Timeout)
                .Ignore(UserTrigger.Connect);

            userStateMachine.Configure(UserState.Joined)
                .OnEntry(StartDisconnectTimer)
                .Permit(UserTrigger.Connect, UserState.Playing)
                .Permit(UserTrigger.Timeout, UserState.Timeout)
                .Ignore(UserTrigger.Join)
                .Ignore(UserTrigger.Disconnect);

            userStateMachine.Configure(UserState.Playing)
                .Permit(UserTrigger.Disconnect, UserState.Joined)
                .Ignore(UserTrigger.Join)
                .Ignore(UserTrigger.Timeout)
                .Ignore(UserTrigger.Connect);

            userStateMachine.Configure(UserState.Timeout)
                .SubstateOf(UserState.Created)
                .OnEntry(() => UserTimeout?.Invoke(this));
        }

        public User(string id, ILoggerFactory loggerFactory) : this()
        {
            Id = id;
            logger = loggerFactory.CreateLogger($"User {Id}");
        }

        public void Connect()
        {
            logger.LogDebug("Connect");
            userStateMachine.Fire(UserTrigger.Connect);
            UserConnected?.Invoke(this);
        }

        public void Disconnect()
        {
            logger.LogDebug("Disconnect");
            userStateMachine.Fire(UserTrigger.Disconnect);
            UserDisconnected?.Invoke(this);
        }

        public void Join()
        {
            logger.LogDebug("Join");
            userStateMachine.Fire(UserTrigger.Join);
        }

        public void UpdateFromClaims(ClaimsPrincipal claims)
        {
            UserName = claims.FindFirst("name")?.Value ?? UserName;
            Email = claims.FindFirst("emails")?.Value ?? Email;
        }

        private void StartDisconnectTimer()
        {
            logger.LogDebug("StartDisconnectTimer");
            disconnectTimer?.Change(Infinite, Infinite);
            disconnectTimer?.Dispose();
            disconnectTimer = new Timer(Timeout, null, 15 * 1000, Infinite);
        }

        private void Timeout(object ignored)
        {
            logger.LogDebug("Timeout");
            userStateMachine.Fire(UserTrigger.Timeout);
        }

        public enum UserState
        {
            Created,
            Connected,
            Joined,
            Playing,
            Timeout,
        }

        public enum UserTrigger
        {
            Connect,
            Join,
            Disconnect,
            Timeout,
        }
    }


}