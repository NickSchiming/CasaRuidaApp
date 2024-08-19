using QRCoder;
using System.Net;

namespace CasaRuidaApp
{
    public partial class MainPage
    {
        private readonly HttpListener _listener = new();
        public bool ContinueListening = true;
        private readonly string _url = MauiProgram.Url;
        private readonly string _callbackurl = MauiProgram.CallbackUrl;
        private readonly string? _error;

        //public static MainPage Instance = null!;

        public MainPage()
        {
            InitializeComponent();
            //Instance = this;

            Loaded += MainPage_Loaded!;

            if (_url.Equals("Erro"))
            {
                _error = "Não foi possível pegar endereço de IP do dispositivo!";
                return;
            }

            _listener.Prefixes.Add(_url);
            _listener.Prefixes.Add(_callbackurl);

        }

        protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
        {
            base.OnNavigatingFrom(args);
            StopListening();
        }

        private void MainPage_Loaded(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_error))
            {
                DisplayAlert("Erro", _error, "OK");
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (DeviceInfo.Platform != DevicePlatform.WinUI)
            {
                App.spotConnection.AccessToken = Preferences.Get("Token", null);
                App.spotConnection.RefreshToken = Preferences.Get("RefreshToken", null);
            }
            

            if (App.spotConnection.AccessToken != null)
            {
                App.spotConnection.AccessTokenSet = true;
                Dispatcher.Dispatch(() =>
                    {
                        Shell.Current.GoToAsync(nameof(LoopPage));
                    });
            }
            else
            {
                if (!_listener.IsListening)
                {
                    _listener.Start();
                }

                await UpdateOutputLabel();

                await HandleRequests();
            }
        }

        private async void OnConnectOtherClicked(object sender, EventArgs e)
        {
            if (!_listener.IsListening)
            {
                _listener.Start();
            }

            await UpdateOutputLabel();

            await HandleRequests();
        }

        private async Task UpdateOutputLabel()
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(MauiProgram.Url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);

            await Dispatcher.DispatchAsync(() =>
            {
                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += async (_, _) => await Launcher.OpenAsync(new Uri(_url));

                QRCode.Source = ImageSource.FromStream(() => new MemoryStream(qrCodeImage));

                OutputLabel.FormattedText = new FormattedString
                {
                    Spans = {
                new Span { Text = "Por favor acessar esse site: " },
                new Span { Text = _url, TextColor = Colors.Blue, TextDecorations = TextDecorations.Underline, GestureRecognizers = { tapGestureRecognizer } },
                new Span { Text = " no navegador" }
            }
                };
            });
        }


        private async Task HandleRequests()
        {
            while (ContinueListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    if (request.RawUrl?.StartsWith("/callback") == true)
                    {
                        await HandleCallbackRequest(response, request);
                    }
                    if (request.RawUrl == "/")
                    {
                        ShowAuthPage(response, _callbackurl);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async Task HandleCallbackRequest(HttpListenerResponse response, HttpListenerRequest request)
        {
            if (!App.spotConnection.AccessTokenSet)
            {
                var code = request.QueryString["code"];
                if (code == null)
                {
                    ShowAuthPage(response, _callbackurl, true);
                }
                else
                {
                    await App.spotConnection.GetUserCode(code);
                    if (DeviceInfo.Platform != DevicePlatform.WinUI)
                    {
                        Preferences.Set("Token", App.spotConnection.AccessToken);
                        Preferences.Set("RefreshToken", App.spotConnection.RefreshToken);
                    }
                    await Dispatcher.DispatchAsync(async () =>
                    {
                        await Shell.Current.GoToAsync(nameof(LoopPage));
                    });
                }
            }
            else
            {
                await Dispatcher.DispatchAsync(async () =>
                {
                    await Shell.Current.GoToAsync(nameof(LoopPage));
                });
            }
        }

        public void StopListening()
        {
            ContinueListening = false;
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
        }

        private static async void ShowAuthPage(HttpListenerResponse response, string callbackUrl, bool erro = false)
        {
            const string scopes = "user-read-currently-playing user-read-playback-state";
            var responseString = $$"""
                                   
                                                           <html>
                                                               <head>
                                                                   <style>
                                                                       body {
                                                                           display: flex;
                                                                           justify-content: center;
                                                                           align-items: center;
                                                                           height: 100vh;
                                                                           margin: 0;
                                                                           background-color: #121212;
                                                                           color: white;
                                                                           font-family: Arial, sans-serif;
                                                                       }
                                                                       .auth-button {
                                                                           font-size: 2em;
                                                                           padding: 20px 40px;
                                                                           text-decoration: none;
                                                                           background-color: #1DB954;
                                                                           color: white;
                                                                           border-radius: 50px;
                                                                           transition: background-color 0.5s;
                                                                       }
                                                                       .auth-button:hover {
                                                                           background-color: #1ED760;
                                                                       }
                                                                   </style>
                                                               </head>
                                                               <body>
                                                                   {{(erro ? "<h1>Erro ao autorizar o Spotify, tente noavemente</h1>" : "")}}
                                                                   <a href='https://accounts.spotify.com/authorize?response_type=code&client_id={{MauiProgram.ClientId}}&redirect_uri={{callbackUrl}}&scope={{scopes}}' class='auth-button'>Clique para autorizar o Spotify</a>
                                                               </body>
                                                           </html>
                                   """;



            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            await output.WriteAsync(buffer);
            output.Close();
        }
    }
}
