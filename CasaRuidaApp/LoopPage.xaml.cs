using CasaRuidaApp.Util;
using System.Diagnostics;
using System.Security.Cryptography;

namespace CasaRuidaApp
{
    public partial class LoopPage : ContentPage
    {
        private long refreshLoop = 0;
        private string currentImageHash;
        public LoopPage()
        {
            InitializeComponent();
            Shell.SetNavBarIsVisible(this, false);
            StartLoop();
        }

        private async void StartLoop()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (App.SpotConnection.accessTokenSet)
                    {
                        if (MainPage.Instance?.continueListening == true)
                        {
                            MainPage.Instance.StopListening();
                        }
                        if ((App.stopwatch.ElapsedMilliseconds - App.SpotConnection.tokenStartTime) / 1000 > App.SpotConnection.tokenExpireTime)
                        {
                            Console.WriteLine("Refreshing token");
                            App.SpotConnection.RefreshAuth();
                        }
                        if ((App.stopwatch.ElapsedMilliseconds - refreshLoop) > 2000)
                        {
                            App.SpotConnection.GetTrackInfo();
                            refreshLoop = App.stopwatch.ElapsedMilliseconds;
                            await this.Dispatcher.DispatchAsync(async () =>
                            {
                                if (App.SpotConnection.AlbumImagePath != null)
                                {
                                    string newImageHash = ComputeSha256Hash(App.SpotConnection.AlbumImagePath);
                                    if (currentImageHash != newImageHash)
                                    {
                                        this.AlbumCover.Source = App.SpotConnection.AlbumImagePath;
                                        currentImageHash = newImageHash;
                                    }
                                    
                                    if (this.ArtistName.Text.Length > 30)
                                        this.ArtistName.Text = App.SpotConnection.currentSong.artist.Substring(0, 28) + "...";
                                    else
                                        this.ArtistName.Text = App.SpotConnection.currentSong.artist;

                                    this.SongName.Text = App.SpotConnection.currentSong.song;

                                    this.ProgressBar.Progress = App.SpotConnection.currentSongPositionMs / App.SpotConnection.currentSong.durationMs;
                                }
                            });
                        }
                    }
                    else
                    {
                        this.Dispatcher.Dispatch(async () =>
                        {
                            await Shell.Current.GoToAsync("///MainPage");
                        });
                    }
                }
            });
        }

        private string ComputeSha256Hash(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
