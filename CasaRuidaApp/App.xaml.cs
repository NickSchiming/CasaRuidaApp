using CasaRuidaApp.Util;
using System.Diagnostics;

namespace CasaRuidaApp
{
    public partial class App
    {
        public static SpotConn spotConnection { get; private set; } = null!;
        public static Stopwatch stopwatch { get; private set; } = null!;

        public App()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(LoopPage), typeof(LoopPage));
            MainPage = new AppShell();
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var directoryPath = Path.Combine(currentDirectory, "resources", "images");
            Directory.CreateDirectory(directoryPath);
            spotConnection = new SpotConn(directoryPath);
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

    }
}
