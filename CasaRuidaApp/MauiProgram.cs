using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static QRCoder.PayloadGenerator;

namespace CasaRuidaApp
{
    public static class MauiProgram
    {
        public const string ClientId = "2b0326e4249f44b8b4f1a6166612f96b";
        public const string ClientSecret = "bfccecc6d5194525ab83cb7a3665f479";
        public static readonly string Url = GetLocalIpAddress();
        public static readonly string CallbackUrl = Url + "callback";

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

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static string GetLocalIpAddress()
        {
            var ipAddress = NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)?
                .Address.ToString();

            return ipAddress ?? "Erro";
        }
    }
}