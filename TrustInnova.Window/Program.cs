using Serilog.Events;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using TrustInnova.Service.Chat;
using TrustInnova.Service.KBS;
using TrustInnova.Service;
using MudBlazor.Services;
using TrustInnova.Abstractions;
using TrustInnova.Provider.Baidu;
using TrustInnova.Provider.OpenAI;
using TrustInnova.Provider.XunFei;
using TrustInnova.Application.DataStorage;
using TrustInnova.Application.DB;
using TrustInnova.Application.AIAssistant.Entities;
using TrustInnova.Application.AIAssistant;
using TrustInnova.Application.Provider;
using TrustInnova.Provider.Ollama;
using TrustInnova.Provider.LLama;

namespace TrustInnova
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
                .MinimumLevel.Override("TrustInnova", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();

            var services = new ServiceCollection();
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            services.AddSingleton<IDataStorageService>(s =>
            {
                return new FileStorageService(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrustInnova"));
            });

            services.AddSingleton<DBData<AssistantEntity>>();
            services.AddSingleton<AIAssistantService>();

            services.AddScoped<IChatService, LocalChatService>();
            services.AddScoped<IKBSService, LocalKBSService>();

            services.AddProviderRegisterer()
                    .RegistererBaiduProvider()
                    .RegistererOpenAIProvider()
                    .RegistererXunFeiProvider()
                    .RegistererOllamaProvider()
                    .RegistererLLamaProvider();

            services.AddSingleton<ProviderService>();

            services.AddMudServices();

            services.AddMasaBlazor();

            services.AddWindowsFormsBlazorWebView();
#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
#endif
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            System.Windows.Forms.Application.Run(new MainForm(services.BuildServiceProvider()));
        }
    }
}