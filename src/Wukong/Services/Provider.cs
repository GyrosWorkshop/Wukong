using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

using Wukong.Models;
using Wukong.Options;


namespace Wukong.Services
{
    public interface IProvider
    {
        Task<List<SongInfo>> Search(SearchSongRequest query);
        Task<Song> GetSong(ClientSong clientSong, bool requestUrl = false, string ip = null);
    }

    class Provider: IProvider
    {
        private HttpClient client;
        private JsonMediaTypeFormatter formatter;

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
        }

        public async Task<List<SongInfo>> Search(SearchSongRequest query)
        {
            try
            {
                var response = await client.PostAsync("api/searchSongs", query, formatter);
                if (response.IsSuccessStatusCode)
                {
                    var songList = await response.Content.ReadAsAsync<List<SongInfo>>();
                    return songList;
                }
            } catch (Exception) { }
            return null;
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
            try
            {
                var response = await client.PostAsync<GetSongRequest>("api/songInfo", request, formatter);
                if (response.IsSuccessStatusCode)
                {
                    var song = await response.Content.ReadAsAsync<Song>();
                    return song;
                }
            } catch (Exception) { }
            return null;
        }
    }
}