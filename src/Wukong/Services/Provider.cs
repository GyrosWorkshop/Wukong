using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

using Wukong.Models;

namespace Wukong.Services
{
    class Provider
    {
        private HttpClient client;
        private JsonMediaTypeFormatter formatter;

        public Provider(string url)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(url);
            formatter = new JsonMediaTypeFormatter();
            formatter.SerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public async Task<List<SongInfo>> Search(SearchSongRequest query)
        {
            var response = await client.PostAsync<SearchSongRequest>("api/searchSongs", query, formatter);
            if (response.IsSuccessStatusCode)
            {
                var songList = await response.Content.ReadAsAsync<List<SongInfo>>();
                return songList;
            }
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
            var response = await client.PostAsync<GetSongRequest>("api/songInfo", request, formatter);
            if (response.IsSuccessStatusCode)
            {
                var song = await response.Content.ReadAsAsync<Song>();
                return song;
            }
            return null;
        }
    }
}