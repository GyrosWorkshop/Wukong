namespace Wukong.Options
{
    public class AzureAdB2COptions
    {
        public string Authority => $"{Instance}/{Tenant}/v2.0";

        public string Instance { get; set; }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Tenant { get; set; }
        public string SignInPolicyId { get; set; }
    }
}