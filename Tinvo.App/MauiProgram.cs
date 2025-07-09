using Tinvo.Service.Chat;
using Tinvo.Service.KBS;
using Tinvo.Service;
using Serilog;
using Serilog.Events;
using Tinvo.Application.DataStorage;
using MudBlazor.Services;
using Microsoft.Extensions.Logging;
using Tinvo.Abstractions;
using Tinvo.Provider.OpenAI;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.DB;
using Tinvo.Application.Provider;
using Tinvo.Application;
using Tinvo.Provider.MCP;

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
                return new DataStorageServiceFactory(new FileStorageService(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tinvo")), s.GetRequiredService<ICryptographyService>());
            });

            services.AddSingleton<DBData<AssistantEntity>>();
            services.AddSingleton<AIAssistantService>();

            services.AddScoped<IChatService, LocalChatService>();
            services.AddScoped<IKBSService, LocalKBSService>();

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