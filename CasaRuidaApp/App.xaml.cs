using CasaRuidaApp.Util;
using System.Diagnostics;

namespace CasaRuidaApp
{
    public partial class App : Application
    {
        public static SpotConn SpotConnection { get; set; }
        public static Stopwatch stopwatch { get; set; }
        public App()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(LoopPage), typeof(LoopPage));
            MainPage = new AppShell();
            SpotConnection = new SpotConn();
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

    }
}
