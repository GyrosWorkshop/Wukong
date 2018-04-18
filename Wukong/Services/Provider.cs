using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Wukong.Models;
using Wukong.Options;


namespace Wukong.Services
{
    public interface IProvider
    {
        Task<List<SongInfo>> Search(SearchSongRequest query);
        Task<Song> GetSong(ClientSong clientSong, bool requestUrl = false);
        Task<object> ApiProxy(string feature, object param, string ip = null);
    }

    class Provider : IProvider
    {
        private readonly HttpClient client;
        private readonly HttpClient getSongClient;
        private readonly JsonMediaTypeFormatter formatter;
        private readonly TelemetryClient telemetry;
        private readonly ILogger logger;

        public Provider(IOptions<SettingOptions> option, ILoggerFactory loggerFactory)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(option.Value.Provider.Url);
            
            // Special option for getSongClient.
            getSongClient = new HttpClient();
            getSongClient.BaseAddress = new Uri(option.Value.Provider.Url);
            getSongClient.Timeout = TimeSpan.FromSeconds(30);
            
            formatter = new JsonMediaTypeFormatter();
            formatter.SerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            telemetry = new TelemetryClient();
            logger = loggerFactory.CreateLogger<Provider>();
        }

        public async Task<List<SongInfo>> Search(SearchSongRequest query)
        {
            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            List<SongInfo> result = null;
            try
            {
                var response = await client.PostAsync("api/searchSongs", query, formatter);
                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsAsync<List<SongInfo>>();
                }
            }
            catch (Exception)
            {
                // ignored
            }
            timer.Stop();
            telemetry.TrackDependency("Provider", "Search", startTime, timer.Elapsed, result != null);
            return result;
        }

        public async Task<Song> GetSong(ClientSong clientSong, bool requestUrl = false)
        {
            if (clientSong == null)
            {
                return null;
            }
            var request = new GetSongRequest
            {
                SiteId = clientSong.SiteId,
                SongId = clientSong.SongId,
                WithFileUrl = requestUrl,
                WithCookie = clientSong.WithCookie
            };

            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Song result = null;
            int retryCount = 2, currentRetry = 0;
            while (result == null && currentRetry <= retryCount)
            {
                try
                {
                    var response = await getSongClient.PostAsync("api/songInfo", request, formatter);
                    if (response.IsSuccessStatusCode)
                    {
                        result = await response.Content.ReadAsAsync<Song>();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogTrace(new EventId(), ex, "An error occurred from GetSong {0} {1} {2}", clientSong.SiteId, clientSong.SongId, currentRetry);
                }
                currentRetry++;
            }
            
            timer.Stop();
            telemetry.TrackDependency("Provider", "GetSong", startTime, timer.Elapsed, result != null);

            return result;
        }

        public async Task<object> ApiProxy(string feature, object param, string ip = null)
        {
            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            object result = null;
            try
            {
                var response = await client.PostAsync("api/" + feature, param, formatter);
                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsAsync<object>();
                }
            }
            finally {
                timer.Stop();
                telemetry.TrackDependency("Provider", "ApiProxy " + feature, startTime, timer.Elapsed, result != null);
            }

            return result;
        }
    }
}
