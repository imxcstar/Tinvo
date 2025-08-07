using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using Tinvo.Abstractions;
using Tinvo.Abstractions.DB;
using Tinvo.Application;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.DataStorage;
using Tinvo.Application.Provider;
using Tinvo.Provider.MCP;
using Tinvo.Provider.OpenAI;
using Tinvo.Service;
using Tinvo.Service.Chat;

namespace Tinvo
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Tinvo", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

            var services = builder.Services;

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));


            services.AddSingleton<Tinvo.Application.IPlatform>(s =>
            {
                return new Tinvo.Application.Platform()
                {
                    Type = PlatformType.Maui
                };
            });

            services.AddSingleton<ICryptographyService, MachineFingerprintCryptographyService>();

            services.AddSingleton<IDataStorageServiceFactory>(s =>
            {
                return new DataStorageServiceFactory(s, new FileStorageService(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tinvo")));
            });

            services.AddSingleton<INotification, DefaultNotificationService>();

            services.AddSingleton<LinkedDB<AssistantEntity>>();
            services.AddSingleton<AIAssistantService>();

            services.AddScoped<IChatService, ChatService>();

            services.AddProviderRegisterer()
                .RegistererOpenAIProvider()
                .RegistererMCPProvider();

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