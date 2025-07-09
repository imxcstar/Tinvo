using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using System.Diagnostics.Metrics;
using Tinvo;
using Tinvo.Abstractions;
using Tinvo.Application;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.DataStorage;
using Tinvo.Application.DB;
using Tinvo.Application.Provider;
using Tinvo.Pages.Chat;
using Tinvo.Provider.Baidu;
using Tinvo.Provider.LLama;
using Tinvo.Provider.Ollama;
using Tinvo.Provider.OpenAI;
using Tinvo.Provider.XunFei;
using Tinvo.Service;
using Tinvo.Service.Chat;
using Tinvo.Service.KBS;
using Tinvo.Services;
using Tinvo.Shared;
using Tinvo.Provider.MCP;
using Microsoft.JSInterop;

var renderMode = args.ElementAtOrDefault(0) ?? "Server";

if (renderMode.Equals("wasm", StringComparison.OrdinalIgnoreCase))
{
    GlobalConfig.RenderMode = Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveWebAssembly;
    renderMode = "WebAssembly";
}

Console.WriteLine("RenderMode: " + renderMode);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
#if DEBUG
    .MinimumLevel.Override("Tinvo", LogEventLevel.Debug)
#endif
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var services = builder.Services;


services.AddSingleton<IPlatform>(s =>
{
    return new Platform()
    {
        Type = PlatformType.WebServer
    };
});

services.AddSingleton<ICryptographyService, MachineFingerprintCryptographyService>();

services.AddScoped<IDataStorageServiceFactory>(s =>
{
    var jsRuntime = s.GetRequiredService<IJSRuntime>();
    return new DataStorageServiceFactory(new LocalForageService(jsRuntime), s.GetRequiredService<ICryptographyService>());
});

services.AddScoped<DBData<AssistantEntity>>();
services.AddScoped<AIAssistantService>();

services.AddScoped<IChatService, LocalChatService>();
services.AddScoped<IKBSService, LocalKBSService>();

services.AddProviderRegisterer()
        .RegistererBaiduProvider()
        .RegistererOpenAIProvider()
        .RegistererXunFeiProvider()
        .RegistererOllamaProvider()
        .RegistererLLamaProvider()
        .RegistererMCPProvider();

services.AddScoped<ProviderService>();

services.AddMudServices();

services.AddMasaBlazor();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Main).Assembly);

app.Run();
