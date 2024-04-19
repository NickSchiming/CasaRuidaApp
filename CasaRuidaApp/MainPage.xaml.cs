using CasaRuidaApp.Util;
using RestSharp;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static System.Net.WebRequestMethods;

namespace CasaRuidaApp
{
    public partial class MainPage : ContentPage
    {
        private HttpListener listener;
        private string url;
        private string callbackUrl;
        public bool continueListening = true;
        
        public static MainPage Instance { get; private set; }

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

            if (!listener.IsListening)
            {
                listener.Start();
            }

            await this.Dispatcher.DispatchAsync(() =>
            {
                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += async (s, ee) => await Launcher.OpenAsync(new Uri(url));

                OutputLabel.FormattedText = new FormattedString
                {
                    Spans = {
                new Span { Text = "Por favor acessar esse site: " },
                new Span { Text = url, TextColor = Colors.Blue, TextDecorations = TextDecorations.Underline, GestureRecognizers = { tapGestureRecognizer } },
                new Span { Text = " no navegador" }
            }
                };
            });

            await HandleRequests();
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

                    if (request.RawUrl.StartsWith("/callback"))
                    {

                        if (!App.SpotConnection.accessTokenSet)
                        {
                            string code = request.QueryString["code"];
                            if (code == null)
                            {
                                ShowAuthPage(response, callbackUrl);
                            }
                            else
                            {
                                App.SpotConnection.GetUserCode(code);
                                await this.Dispatcher.DispatchAsync(async () =>
                                {
                                    await Shell.Current.GoToAsync(nameof(LoopPage));
                                });

                            }
                        }
                        else
                        {
                            await this.Dispatcher.DispatchAsync(() =>
                            {
                                OutputLabel.Text = "A autorização foi bem-sucedida!";
                            });
                        }
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

        public void StopListening()
        {
            continueListening = false;
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }


        private async void ShowAuthPage(HttpListenerResponse response, string callbackUrl)
        {
            string scopes = "user-read-currently-playing user-read-playback-state";
            string responseString = $"<html><body><a href='https://accounts.spotify.com/authorize?response_type=code&client_id={MauiProgram.clientId}&redirect_uri={callbackUrl}&scope={scopes}' style='font-size:50px;'>Clique para autorizar o Spotify </a></body></html>";

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}
