namespace Wukong.Options
{
    public class SettingOptions
    {
        public ApplicationInsightsOptions ApplicationInsights { get; set; }
        public ProviderOptions Provider { get; set; }
        public AzureAdB2COptions AzureAdB2COptions { get; set; }
        public AzureAdB2CPolicies AzureAdB2CPolicies { get; set; }

        public string SqliteConnectionString { get; set; }
        public string RedisConnectionString { get; set; }
        public string AzureStorageConnectionString { get; set; }
        public string WukongOrigin { get; set; }
    }
}