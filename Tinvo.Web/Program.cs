using Tinvo;
using Tinvo.Pages.Chat;
using Tinvo.Service.Chat;
using Tinvo.Service;
using MudBlazor.Services;
using System.Diagnostics.Metrics;
using Tinvo.Shared;
using Tinvo.Service.KBS;
using Serilog;
using Serilog.Events;
using Tinvo.Services;
using Tinvo.Application.DataStorage;
using Tinvo.Abstractions;
using Tinvo.Provider.Baidu;
using Tinvo.Provider.OpenAI;
using Tinvo.Provider.XunFei;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.DB;
using Tinvo.Application.Provider;
using Tinvo.Provider.Ollama;
using Tinvo.Provider.LLama;

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
    .MinimumLevel.Override("Tinvo", LogEventLevel.Debug)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var services = builder.Services;

services.AddScoped<IDataStorageService, LocalForageService>();

services.AddScoped<DBData<AssistantEntity>>();
services.AddScoped<AIAssistantService>();

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
