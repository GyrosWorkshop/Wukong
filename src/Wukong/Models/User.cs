using System.Security.Claims;

namespace Wukong.Models
{
    public class User
    {
        public static readonly string AvartaKey = "avarta";
        string userName;
        string userId;
        string avatar;

        public User(string userId)
        {
            this.userId = userId;
        }

        public string UserName
        {
            get
            {
                return userName;
            }

            set
            {
                userName = value;
            }
        }

        public string Id
        {
            get
            {
                return userId;
            }
        }

        public string Avatar
        {
            get
            {
                return avatar;
            }

            set
            {
                avatar = value;
            }
        }

        public void UpdateFromClaims(ClaimsPrincipal claim)
        {
            UserName = claim.FindFirst(ClaimTypes.Name)?.Value ?? UserName;
            Avatar = claim.FindFirst(AvartaKey)?.Value ?? Avatar;
        }
    }
}