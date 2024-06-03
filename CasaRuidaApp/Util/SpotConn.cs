using CasaRuidaApp.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace CasaRuidaApp.Util
{
    public class SpotConn
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        public string AccessToken = string.Empty;
        public string RefreshToken = string.Empty;
        public bool AccessTokenSet;
        public long TokenStartTime;
        public int TokenExpireTime;
        public SongDetails CurrentSong = new SongDetails();
        public float CurrentSongPositionMs;
        public string? AlbumImagePath { get; private set; }
        private string DirectoryPath { get; }

        public SpotConn(string directoryPath)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this.DirectoryPath = directoryPath;
        }

        private async Task<string> SendRequestAsync(string url, HttpMethod method, string auth = "", HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(auth))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            }
            if (content != null)
            {
                request.Content = content;
            }

            HttpResponseMessage response = await HttpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task GetUserCode(string code)
        {
            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MauiProgram.ClientId}:{MauiProgram.ClientSecret}"));
            string requestBody = $"grant_type=authorization_code&code={code}&redirect_uri={MauiProgram.RedirectUri}";
            var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");
            string response = await SendRequestAsync("https://accounts.spotify.com/api/token", HttpMethod.Post, auth, content);

            var token = JsonConvert.DeserializeObject<Token>(response);
            if (token != null)
            {
                AccessToken = token.access_token;
                RefreshToken = token.refresh_token;
                AccessTokenSet = true;
                TokenStartTime = App.Stopwatch.ElapsedMilliseconds;
                TokenExpireTime = token.expires_in;
            }
        }

        public async Task RefreshAuth()
        {
            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MauiProgram.ClientId}:{MauiProgram.ClientSecret}"));
            string requestBody = $"grant_type=refresh_token&refresh_token={RefreshToken}";
            var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");
            string response = await SendRequestAsync("https://accounts.spotify.com/api/token", HttpMethod.Post, auth, content);

            var token = JsonConvert.DeserializeObject<Token>(response);
            if (token != null)
            {
                AccessToken = token.access_token;
                AccessTokenSet = true;
                TokenStartTime = App.Stopwatch.ElapsedMilliseconds;
                TokenExpireTime = token.expires_in;
            }
        }

        public async Task<bool> GetTrackInfo()
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                return false;
            }

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            string response = await HttpClient.GetStringAsync("https://api.spotify.com/v1/me/player/currently-playing");

            var currentlyPlaying = JsonConvert.DeserializeObject<CurentlyPlaying>(response);
            if (currentlyPlaying == null) return false;

            CurrentSong.durationMs = currentlyPlaying.item.duration_ms;
            CurrentSongPositionMs = currentlyPlaying.progress_ms;
            string imageLink = currentlyPlaying.item.album.images[0].url;

            string localPath = Path.Combine(DirectoryPath, $"albumImage_{DateTime.Now.Ticks}.png");

            if (File.Exists(AlbumImagePath))
            {
                File.Delete(AlbumImagePath);
            }

            await DownloadAlbumImageAsync(imageLink, localPath);

            AlbumImagePath = localPath;

            CurrentSong.artist = currentlyPlaying.item.artists[0].name;
            CurrentSong.song = currentlyPlaying.item.name;
            CurrentSong.durationMs = currentlyPlaying.item.duration_ms;
            return true;
        }

        private static async Task DownloadAlbumImageAsync(string imageLink, string localPath)
        {
            HttpResponseMessage response = await HttpClient.GetAsync(imageLink);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream);
            }
        }
    }
}
