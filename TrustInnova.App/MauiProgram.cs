using TrustInnova.Service.Chat;
using TrustInnova.Service.KBS;
using TrustInnova.Service;
using Serilog;
using Serilog.Events;
using TrustInnova.Application.DataStorage;
using MudBlazor.Services;
using Microsoft.Extensions.Logging;
using TrustInnova.Abstractions;
using TrustInnova.Provider.Baidu;
using TrustInnova.Provider.OpenAI;
using TrustInnova.Provider.XunFei;
using TrustInnova.Application.AIAssistant.Entities;
using TrustInnova.Application.AIAssistant;
using TrustInnova.Application.DB;
using TrustInnova.Application.Provider;
using TrustInnova.Provider.LLama;
using TrustInnova.Provider.Ollama;

namespace TrustInnova
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("TrustInnova", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            var services = builder.Services;

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

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
