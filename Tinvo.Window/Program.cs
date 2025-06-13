using Serilog;
using Serilog.Events;
using WinFormedge;

namespace Tinvo
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Tinvo", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();

            ApplicationConfiguration.Initialize();

            var app = WinFormedgeApp.CreateAppBuilder()
                .UseCulture(System.Windows.Forms.Application.CurrentCulture.Name)
                .UseDevTools()
                .UseModernStyleScrollbar()
                .UseWinFormedgeApp<TinvoApp>()
                .Build();

            app.Run();
        }
    }
}