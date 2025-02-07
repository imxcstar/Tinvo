using Serilog.Events;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Tinvo.Service.Chat;
using Tinvo.Service.KBS;
using Tinvo.Service;
using MudBlazor.Services;
using Tinvo.Abstractions;
using Tinvo.Provider.Baidu;
using Tinvo.Provider.OpenAI;
using Tinvo.Provider.XunFei;
using Tinvo.Application.DataStorage;
using Tinvo.Application.DB;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.Provider;
using Tinvo.Provider.Ollama;
using Tinvo.Provider.LLama;
using Tinvo.Provider.Onnx;

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

            var services = new ServiceCollection();
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            services.AddSingleton<IDataStorageService>(s =>
            {
                return new FileStorageService(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tinvo"));
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
                    .RegistererLLamaProvider()
                    .RegistererOnnxProvider();

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