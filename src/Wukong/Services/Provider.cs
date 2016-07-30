using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Wukong.Models;
using Wukong.Options;


namespace Wukong.Services
{
    public interface IProvider
    {
        Task<List<SongInfo>> Search(SearchSongRequest query);
        Task<Song> GetSong(ClientSong clientSong, bool requestUrl = false, string ip = null);
        Task<object> ApiProxy(string feature, object param, string ip = null);
    }

    class Provider : IProvider
    {
        private readonly HttpClient client;
        private readonly JsonMediaTypeFormatter formatter;
        private readonly TelemetryClient Telemetry;

        public Provider(IOptions<ProviderOption> option)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(option.Value.Url);
            formatter = new JsonMediaTypeFormatter();
            formatter.SerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            Telemetry = new TelemetryClient();
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
            catch (Exception) { }
            timer.Stop();
            Telemetry.TrackDependency("Provider", "Search", startTime, timer.Elapsed, result != null);
            return result;
        }

        public async Task<Song> GetSong(ClientSong clientSong, bool requestUrl = false, string ip = null)
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
                ClientIP = ip,
            };

            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Song result = null;
            int retryCount = 3, currentRetry = 0;
            // tripple retries max
            for (; ;)
            {
                try
                {
                    var response = await client.PostAsync("api/songInfo", request, formatter);
                    if (response.IsSuccessStatusCode)
                    {
                        result = await response.Content.ReadAsAsync<Song>();
                        if (result == null) throw new Exception("song null");
                    }
                }
                catch (Exception ex)
                {
                    currentRetry++;
                    if (currentRetry > retryCount)
                    {
                        break;
                    }
                }
            }
            
            timer.Stop();
            Telemetry.TrackDependency("Provider", "GetSong", startTime, timer.Elapsed, result != null);
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
                Telemetry.TrackDependency("Provider", "ApiProxy " + feature, startTime, timer.Elapsed, result != null);
            }

            return result;
        }
    }
}