using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CasaRuidaApp
{
    public static class MauiProgram
    {
        public static string clientId = "2b0326e4249f44b8b4f1a6166612f96b";
        public static string clientSecret = "bfccecc6d5194525ab83cb7a3665f479";
        public static string redirectUri = "";
        public static string localIp = "";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            localIp = GetLocalIPAddress();
            redirectUri = $"http://{localIp}:8040/callback/";

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static string GetLocalIPAddress()
        {
            var ipAddress = NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)?
                .Address.ToString();

            return ipAddress ?? string.Empty;
        }
    }
}