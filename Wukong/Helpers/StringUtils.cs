using System.Security.Cryptography;
using System.Text;

namespace Wukong.Helpers
{
    public static class StringUtils
    {
        public static string MD5String(this string str)
        {
            var hash = new StringBuilder();
            var bytes = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(str));
            foreach (var t in bytes)
            {
                hash.Append(t.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}