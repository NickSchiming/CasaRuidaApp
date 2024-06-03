using CasaRuidaApp.Util;
using System.Diagnostics;

namespace CasaRuidaApp
{
    public partial class App
    {
        public static SpotConn SpotConnection { get; set; } = null!;
        public static Stopwatch Stopwatch { get; set; } = null!;

        public App()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(LoopPage), typeof(LoopPage));
            MainPage = new AppShell();
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string directoryPath = Path.Combine(currentDirectory, "resources", "images");
            Directory.CreateDirectory(directoryPath);
            SpotConnection = new SpotConn(directoryPath);
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
        }

    }
}
