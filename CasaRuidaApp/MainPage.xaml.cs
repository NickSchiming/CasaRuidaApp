using QRCoder;
using System.Net;

namespace CasaRuidaApp
{
    public partial class MainPage
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly string _callbackUrl;
        public bool ContinueListening = true;

        public static MainPage? Instance { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            Instance = this;
            _url = $"http://{MauiProgram.LocalIp}:8040/";
            _callbackUrl = MauiProgram.RedirectUri;
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Prefixes.Add(_callbackUrl);
            
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (DeviceInfo.Platform != DevicePlatform.WinUI)
            {
                App.SpotConnection.AccessToken = Xamarin.Essentials.Preferences.Get("Token", null);
                App.SpotConnection.RefreshToken = Xamarin.Essentials.Preferences.Get("RefreshToken", null);
            }
            

            if (App.SpotConnection.AccessToken != null)
            {
                App.SpotConnection.AccessTokenSet = true;
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
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(_url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            await this.Dispatcher.DispatchAsync(() =>
            {
                 

                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += async (_, _) => await Launcher.OpenAsync(new Uri(_url));

                this.QRCode.Source = ImageSource.FromStream(() =>
                {
                    return new MemoryStream(qrCodeImage);
                });

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
                    HttpListenerContext context = await _listener.GetContextAsync();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    if (request.RawUrl?.StartsWith("/callback") == true)
                    {
                        await HandleCallbackRequest(response, request);
                    }
                    if (request.RawUrl == "/")
                    {
                        ShowAuthPage(response, _callbackUrl);
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
            if (!App.SpotConnection.AccessTokenSet)
            {
                string code = request.QueryString["code"] ?? string.Empty;
                if (code == string.Empty)
                {
                    MainPage.ShowAuthPage(response, _callbackUrl, true);
                }
                else
                {
                    
                    await App.SpotConnection.GetUserCode(code);
                    if (DeviceInfo.Platform != DevicePlatform.WinUI)
                    {
                        Xamarin.Essentials.Preferences.Set("Token", App.SpotConnection.AccessToken);
                        Xamarin.Essentials.Preferences.Set("RefreshToken", App.SpotConnection.RefreshToken);
                    }
                    await this.Dispatcher.DispatchAsync(async () =>
                    {
                        await Shell.Current.GoToAsync(nameof(LoopPage));
                    });
                }
            }
            else
            {
                await this.Dispatcher.DispatchAsync(async () =>
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
            string scopes = "user-read-currently-playing user-read-playback-state";
            string responseString = $$"""
                                      
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



            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}
