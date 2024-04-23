using QRCoder;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection.Emit;

namespace CasaRuidaApp
{
    public partial class MainPage : ContentPage
    {
        private HttpListener listener;
        private string url;
        private string callbackUrl;
        public bool continueListening = true;

        public static MainPage? Instance { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            Instance = this;
            url = $"http://{MauiProgram.localIp}:8040/";
            callbackUrl = MauiProgram.redirectUri;
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Prefixes.Add(callbackUrl);
            
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (Device.RuntimePlatform != DevicePlatform.WinUI.ToString())
            {
                App.SpotConnection.accessToken = Xamarin.Essentials.Preferences.Get("Token", null);
                App.SpotConnection.refreshToken = Xamarin.Essentials.Preferences.Get("RefreshToken", null);
            }
            

            if (App.SpotConnection.accessToken != null)
            {
                App.SpotConnection.accessTokenSet = true;
                this.Dispatcher.Dispatch(async () =>
                    {
                        await Shell.Current.GoToAsync(nameof(LoopPage));
                    });
            }
            else
            {
                if (!listener.IsListening)
                {
                    listener.Start();
                }

                await UpdateOutputLabel();

                await HandleRequests();
            }
        }

        private async void OnConnectOtherClicked(object sender, EventArgs e)
        {
            if (!listener.IsListening)
            {
                listener.Start();
            }

            await UpdateOutputLabel();

            await HandleRequests();
        }

        private async Task UpdateOutputLabel()
        {
            var qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            await this.Dispatcher.DispatchAsync(() =>
            {
                 

                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += async (s, ee) => await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(new Uri(url));

                this.QRCode.Source = ImageSource.FromStream(() =>
                {
                    return new MemoryStream(qrCodeImage);
                });

                OutputLabel.FormattedText = new FormattedString
                {
                    Spans = {
                new Span { Text = "Por favor acessar esse site: " },
                new Span { Text = url, TextColor = Colors.Blue, TextDecorations = TextDecorations.Underline, GestureRecognizers = { tapGestureRecognizer } },
                new Span { Text = " no navegador" }
            }
                };
            });
        }


        private async Task HandleRequests()
        {
            while (continueListening)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    if (request.RawUrl?.StartsWith("/callback") == true)
                    {
                        await HandleCallbackRequest(response, request);
                    }
                    if (request.RawUrl == "/")
                    {
                        ShowAuthPage(response, callbackUrl);
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
            if (!App.SpotConnection.accessTokenSet)
            {
                string code = request.QueryString["code"] ?? string.Empty;
                if (code == string.Empty)
                {
                    ShowAuthPage(response, callbackUrl, true);
                }
                else
                {
                    
                    App.SpotConnection.GetUserCode(code);
                    if (Device.RuntimePlatform != DevicePlatform.WinUI.ToString())
                    {
                        Xamarin.Essentials.Preferences.Set("Token", App.SpotConnection.accessToken);
                        Xamarin.Essentials.Preferences.Set("RefreshToken", App.SpotConnection.refreshToken);
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
            continueListening = false;
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }

        private async void ShowAuthPage(HttpListenerResponse response, string callbackUrl, bool erro = false)
        {
            string scopes = "user-read-currently-playing user-read-playback-state";
            string responseString = $@"
                        <html>
                            <head>
                                <style>
                                    body {{
                                        display: flex;
                                        justify-content: center;
                                        align-items: center;
                                        height: 100vh;
                                        margin: 0;
                                        background-color: #121212;
                                        color: white;
                                        font-family: Arial, sans-serif;
                                    }}
                                    .auth-button {{
                                        font-size: 2em;
                                        padding: 20px 40px;
                                        text-decoration: none;
                                        background-color: #1DB954;
                                        color: white;
                                        border-radius: 50px;
                                        transition: background-color 0.5s;
                                    }}
                                    .auth-button:hover {{
                                        background-color: #1ED760;
                                    }}
                                </style>
                            </head>
                            <body>
                                {(erro ? "<h1>Erro ao autorizar o Spotify, tente noavemente</h1>" : "")}
                                <a href='https://accounts.spotify.com/authorize?response_type=code&client_id={MauiProgram.clientId}&redirect_uri={callbackUrl}&scope={scopes}' class='auth-button'>Clique para autorizar o Spotify</a>
                            </body>
                        </html>";



            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}
