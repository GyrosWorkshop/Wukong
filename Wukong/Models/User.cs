using Stateless;
using System.Security.Claims;
using System.Threading;
using Wukong.Services;
using static System.Threading.Timeout;

namespace Wukong.Models
{

    public class User
    {
        public static readonly string AvatarKey = "avatar";
        
        public static string BuildUserIdentifier(string fromSite, string siteUserId)
        {
            return fromSite + "." + siteUserId;
        }

        public string UserName { get; private set; }

        public string Id => BuildUserIdentifier(Site, OauthId);

        private string OauthId { get; }

        public string Site { get; }

        public string Avatar { get; private set; }

        public string DisplayName { get; private set; }

        public string FromSite { get; private set; }

        public string SiteUserId { get; private set; }

        public string Url { get; private set; }

        private readonly StateMachine<UserState, UserTrigger> _userStateMachine =
            new StateMachine<UserState, UserTrigger>(UserState.Created);

        private Timer _disconnectTimer;

        public event UserStateDelegate UserConnected;
        public event UserStateDelegate UserDisconnected;
        public event UserStateDelegate UserTimeout;

        private User()
        {
            _userStateMachine.Configure(UserState.Created)
                .Permit(UserTrigger.Connect, UserState.Connected)
                .Permit(UserTrigger.Join, UserState.Joined);

            _userStateMachine.Configure(UserState.Connected)
                .Permit(UserTrigger.Join, UserState.Playing)
                .Permit(UserTrigger.Disconnect, UserState.Timeout)
                .Ignore(UserTrigger.Connect);

            _userStateMachine.Configure(UserState.Joined)
                .OnEntry(StartDisconnectTimer)
                .Permit(UserTrigger.Connect, UserState.Playing)
                .Permit(UserTrigger.Timeout, UserState.Created)
                .Ignore(UserTrigger.Join);

            _userStateMachine.Configure(UserState.Playing)
                .Permit(UserTrigger.Disconnect, UserState.Timeout)
                .Ignore(UserTrigger.Join)
                .Ignore(UserTrigger.Connect);

            _userStateMachine.Configure(UserState.Timeout)
                .SubstateOf(UserState.Created)
                .OnEntry(() => UserTimeout?.Invoke(this));
        }

        public User(string userId, string site) : this()
        {
            OauthId = userId;
            Site = site;
        }

        public void Connect()
        {
            _userStateMachine.Fire(UserTrigger.Connect);
            UserConnected?.Invoke(this);
        }

        public void Disconnect()
        {
            _userStateMachine.Fire(UserTrigger.Disconnect);
            UserDisconnected?.Invoke(this);
        }

        public void Join()
        {
            _userStateMachine.Fire(UserTrigger.Join);
        }

        public void UpdateFromClaims(ClaimsPrincipal claims)
        {
            UserName = claims.FindFirst(ClaimTypes.Name)?.Value ?? UserName;
            Avatar = claims.FindFirst(AvatarKey)?.Value ?? Avatar;
            DisplayName = claims.FindFirst(ClaimTypes.Name)?.Value;
            FromSite = claims.FindFirst(ClaimTypes.AuthenticationMethod)?.Value;
            SiteUserId = claims.FindFirst(ClaimTypes.NameIdentifier).Value;
            Url = claims.FindFirst(ClaimTypes.Uri)?.Value;
        }

        private void StartDisconnectTimer()
        {
            _disconnectTimer?.Change(Infinite, Infinite);
            _disconnectTimer?.Dispose();
            _disconnectTimer = new Timer(Timeout, null, 15 * 1000, Infinite);
        }

        private void Timeout(object ignored)
        {
            _userStateMachine.Fire(UserTrigger.Timeout);
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