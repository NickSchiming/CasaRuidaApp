using System.Security.Cryptography;


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
            StartLoop();

            this.Appearing += OnAppearing;
        }

        private void OnAppearing(object? sender, EventArgs e)
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
                if (App.SpotConnection.AccessTokenSet)
                {
                    if (MainPage.Instance?.ContinueListening == true)
                    {
                        MainPage.Instance.StopListening();
                    }

                    if ((App.Stopwatch.ElapsedMilliseconds - App.SpotConnection.TokenStartTime) / 1000 > App.SpotConnection.TokenExpireTime)
                    {
                        await App.SpotConnection.RefreshAuth();
                    }

                    if ((App.Stopwatch.ElapsedMilliseconds - _refreshLoop) > 2000)
                    {
                        _refreshLoop = App.Stopwatch.ElapsedMilliseconds;
                        var ok = await App.SpotConnection.GetTrackInfo();
                        if (ok)
                        {
                            await this.Dispatcher.DispatchAsync(() =>
                            {
                                if (App.SpotConnection.AlbumImagePath != null)
                                {
                                    string newImageHash = ComputeSha256Hash(App.SpotConnection.AlbumImagePath);
                                    if (_currentImageHash != newImageHash)
                                    {
                                        this.AlbumCover.Source = App.SpotConnection.AlbumImagePath;
                                        _currentImageHash = newImageHash;
                                    }
                                }

                                if (this.ArtistName.Text.Length > 30)
                                    this.ArtistName.Text = App.SpotConnection.CurrentSong.artist.Substring(0, 28) + "...";
                                else
                                    this.ArtistName.Text = App.SpotConnection.CurrentSong.artist;

                                this.SongName.Text = App.SpotConnection.CurrentSong.song;

                                this.ProgressBar.Progress = App.SpotConnection.CurrentSongPositionMs / App.SpotConnection.CurrentSong.durationMs;
                            });
                        }
                    }
                }
                else
                {
                    await this.Dispatcher.DispatchAsync(() => { Shell.Current.GoToAsync("///MainPage"); });
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }


        private static string ComputeSha256Hash(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
