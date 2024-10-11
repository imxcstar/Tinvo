using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using System;
using TrustInnova;
using TrustInnova.Abstractions;
using TrustInnova.Application.AIAssistant.Entities;
using TrustInnova.Application.AIAssistant;
using TrustInnova.Application.DataStorage;
using TrustInnova.Application.DB;
using TrustInnova.Provider.Baidu;
using TrustInnova.Provider.OpenAI;
using TrustInnova.Provider.XunFei;
using TrustInnova.Service;
using TrustInnova.Service.Chat;
using TrustInnova.Service.KBS;
using TrustInnova.Services;
using TrustInnova.Application.Provider;
using TrustInnova.Provider.Ollama;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("TrustInnova", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.BrowserConsole()
    .CreateLogger();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var services = builder.Services;
services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

services.AddSingleton<IDataStorageService, LocalForageService>();

services.AddSingleton<DBData<AssistantEntity>>();
services.AddSingleton<AIAssistantService>();

services.AddProviderRegisterer()
        .RegistererBaiduProvider()
        .RegistererOpenAIProvider()
        .RegistererXunFeiProvider()
        .RegistererOllamaProvider();

services.AddSingleton<ProviderService>();

services.AddScoped<IChatService, LocalChatService>();
services.AddScoped<IKBSService, LocalKBSService>();

services.AddMudServices();

services.AddMasaBlazor();

await builder.Build().RunAsync();