using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Wukong.Helpers
{
    public class RedisConnectionUtil
    {
        public static string RedisConnectionDnsLookup(string redisConnectionString)
        {
            ConfigurationOptions config = ConfigurationOptions.Parse(redisConnectionString);

            DnsEndPoint addressEndpoint = config.EndPoints.First() as DnsEndPoint;
            int port = addressEndpoint.Port;

            bool isIp = IsIpAddress(addressEndpoint.Host);
            if (!isIp)
            {
                // Please Don't use this line in blocking context. Please remove ".Result"
                // Just for test purposes
                IPAddress ip = Dns.GetHostEntryAsync(addressEndpoint.Host).Result.AddressList.Last();
                return redisConnectionString.Replace(addressEndpoint.Host, ip.ToString());
            }
            else
            {
                return redisConnectionString;
            }
        }

        // A workaround method.
        private static bool IsIpAddress(string host)
        {
            string ipPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
            return Regex.IsMatch(host, ipPattern);
        }
    }
}
