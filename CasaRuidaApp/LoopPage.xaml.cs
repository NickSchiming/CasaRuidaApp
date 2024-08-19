using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace CasaRuidaApp
{
    public partial class LoopPage
    {
        private long _refreshLoop;
        private string _currentImageHash;

        public LoopPage()
        {
            InitializeComponent();
            Shell.SetNavBarIsVisible(this, false);
            _currentImageHash = string.Empty;
            try
            {
                StartLoop();
            }
            catch (Exception ex)
            {
                DisplayAlert("Erro", ex.Message, "OK");
            }

            Appearing += OnAppearing;
        }

        private static void OnAppearing(object? sender, EventArgs e)
        {
            DeviceDisplay.Current.KeepScreenOn = true;
        }

        private async void StartLoop()
        {
            await Task.Run(LoopApi);
        }

        private async Task LoopApi()
        {
            while (true)
            {
                if (App.spotConnection.AccessTokenSet)
                {
                    if ((App.stopwatch.ElapsedMilliseconds - App.spotConnection.TokenStartTime) / 1000 > App.spotConnection.TokenExpireTime)
                    {
                        await App.spotConnection.RefreshAuth();
                    }

                    if (App.stopwatch.ElapsedMilliseconds - _refreshLoop <= 2000) continue;

                    _refreshLoop = App.stopwatch.ElapsedMilliseconds;
                    if (await App.spotConnection.GetTrackInfo())
                    {
                        await Dispatcher.DispatchAsync(() =>
                        {
                            if (App.spotConnection.albumImagePath != null)
                            {
                                var newImageHash = ComputeSha256Hash(App.spotConnection.albumImagePath);
                                if (_currentImageHash != newImageHash)
                                {
                                    AlbumCover.Source = App.spotConnection.albumImagePath;
                                    _currentImageHash = newImageHash;
                                }
                            }

                            var artistName = App.spotConnection.CurrentSong.artist;

                            if (ArtistName.Text.Length > 30)
                                ArtistName.Text = artistName is { Length: > 30 } ? artistName[..28] + "..." : artistName ?? "Erro pegando nome do artista";

                            SongName.Text = App.spotConnection.CurrentSong.song ?? "Erro pegando nome da musica";

                            ProgressBar.Progress = App.spotConnection.CurrentSongPositionMs / App.spotConnection.CurrentSong.durationMs;
                        });
                    }
                }
                else
                {
                    await Dispatcher.DispatchAsync(() => { Shell.Current.GoToAsync("///MainPage"); });
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }


        private static string ComputeSha256Hash(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
