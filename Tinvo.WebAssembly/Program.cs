using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using System;
using Tinvo;
using Tinvo.Abstractions;
using Tinvo.Application;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.DataStorage;
using Tinvo.Application.DB;
using Tinvo.Application.Provider;
using Tinvo.Provider.Baidu;
using Tinvo.Provider.Ollama;
using Tinvo.Provider.OpenAI;
using Tinvo.Provider.XunFei;
using Tinvo.Service;
using Tinvo.Service.Chat;
using Tinvo.Service.KBS;
using Tinvo.Services;
using Tinvo.Provider.MCP;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Tinvo", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.BrowserConsole()
    .CreateLogger();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var services = builder.Services;
services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

services.AddSingleton<IPlatform>(s =>
{
    return new Platform()
    {
        Type = PlatformType.WebAssembly
    };
});

services.AddSingleton<IDataStorageService, LocalForageService>();

services.AddSingleton<DBData<AssistantEntity>>();
services.AddSingleton<AIAssistantService>();

services.AddProviderRegisterer()
        .RegistererBaiduProvider()
        .RegistererOpenAIProvider()
        .RegistererXunFeiProvider()
        .RegistererOllamaProvider()
        .RegistererMCPProvider();

services.AddSingleton<ProviderService>();

services.AddScoped<IChatService, LocalChatService>();
services.AddScoped<IKBSService, LocalKBSService>();

services.AddMudServices();

services.AddMasaBlazor();

await builder.Build().RunAsync();