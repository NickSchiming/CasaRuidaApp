using CasaRuidaApp.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CasaRuidaApp.Util
{
    public class SpotConn
    {
        #region private
        
        bool isPlaying = false;
        private RestClient client = new RestClient();
        private RestRequest request;
        #endregion

        #region public
        public string accessToken = String.Empty;
        public string refreshToken = String.Empty;
        public bool accessTokenSet = false;
        public long tokenStartTime;
        public int tokenExpireTime;
        public SongDetails currentSong = new SongDetails();
        public float currentSongPositionMs;
        float lastSongPositionMs;
        public string? AlbumImagePath { get; private set; }
        #endregion

        private void PrepareRequest(string resource, string auth, string requestBody)
        {
            request.Resource = resource;
            request.AddHeader("Authorization", auth);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddBody(requestBody);
        }
        public void GetUserCode(string code)
        {
            request = new RestRequest();
            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MauiProgram.clientId}:{MauiProgram.clientSecret}"));
            string requestBody = $"grant_type=authorization_code&code={code}&redirect_uri={MauiProgram.redirectUri}";
            PrepareRequest("https://accounts.spotify.com/api/token", "Basic " + auth, requestBody);

            RestResponse response = client.Execute(request, Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var json = JsonConvert.DeserializeObject<Token>(response.Content);
                accessToken = json.access_token;
                refreshToken = json.refresh_token;
                accessTokenSet = true;
                tokenStartTime = App.stopwatch.ElapsedMilliseconds;
                tokenExpireTime = json.expires_in;
            }

        }

        public void RefreshAuth()
        {
            request = new RestRequest();
            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MauiProgram.clientId}:{MauiProgram.clientSecret}"));
            string requestBody = $"grant_type=refresh_token&refresh_token={refreshToken}";
            PrepareRequest("https://accounts.spotify.com/api/token", "Basic " + auth, requestBody);

            RestResponse response = client.Execute(request, Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var json = JsonConvert.DeserializeObject<Token>(response.Content);
                accessToken = json.access_token;
                //refreshToken = json.refresh_token;
                accessTokenSet = true;
                tokenStartTime = App.stopwatch.ElapsedMilliseconds;
                tokenExpireTime = json.expires_in;
            }


        }

        public void GetTrackInfo()
        {
            request = new RestRequest();
            request.Resource = "https://api.spotify.com/v1/me/player/currently-playing";
            request.AddHeader("Authorization", "Bearer " + accessToken);

            RestResponse response = client.Execute(request, Method.Get);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var json = JsonConvert.DeserializeObject<CurentlyPlaying>(response.Content);
                    currentSong.durationMs = json.item.duration_ms;
                    currentSongPositionMs = json.progress_ms;
                    string imageLink = json.item.album.images[0].url;

                    string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string directoryPath = Path.Combine(currentDirectory, "resources", "images");
                    Directory.CreateDirectory(directoryPath); // Cria o diretório se ele não existir
                    string localPath = Path.Combine(directoryPath, $"albumImage_{DateTime.Now.Ticks}.png");
                    

                    if (File.Exists(AlbumImagePath))
                    {
                        File.Delete(AlbumImagePath);
                    }

                    using (WebClient imageClient = new WebClient())
                    {
                        imageClient.DownloadFile(new Uri(imageLink), localPath);
                    }

                    AlbumImagePath = localPath;

                    currentSong.artist = json.item.artists[0].name;
                    currentSong.song = json.item.name;
                    currentSong.durationMs = json.item.duration_ms;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }
    }
}
