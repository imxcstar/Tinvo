using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.DataStorage;
using Tinvo.Application.DB;
using Tinvo.Application.Provider;
using Tinvo.Service.Chat;
using Tinvo.Service.KBS;
using WinFormedge;
using WinFormedge.Blazor;
using Tinvo.Abstractions;
using Tinvo.Provider.Baidu;
using Tinvo.Provider.LLama;
using Tinvo.Provider.Ollama;
using Tinvo.Provider.Onnx;
using Tinvo.Provider.OpenAI;
using Tinvo.Provider.XunFei;
using Tinvo.Application;
using Tinvo.Provider.MCP;

namespace Tinvo
{
    internal class MainWindow : Formedge
    {
        public MainWindow()
        {
            WindowTitle = "Tinvo";
            Size = new Size(1400, 900);
            Icon = Resources.Icon;
            BackColor = Color.Transparent;
            StartPosition = FormStartPosition.CenterScreen;
            Url = "https://blazorapp.local/";
            Load += MainWindow_Load;
        }

        private void MainWindow_Load(object? sender, EventArgs e)
        {
            this.SetVirtualHostNameToBlazorHybrid(new BlazorHybridOptions
            {
                Scheme = "https",
                HostName = "blazorapp.local",
                RootComponent = typeof(Main),
                HostPath = "wwwroot/index.html",
                ConfigureServices = services =>
                {
                    services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

                    services.AddSingleton<IPlatform>(s =>
                    {
                        return new Platform()
                        {
                            Type = PlatformType.Winformedge
                        };
                    });

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
                            .RegistererOnnxProvider()
                            .RegistererMCPProvider();

                    services.AddSingleton<ProviderService>();

                    services.AddMudServices();

                    services.AddMasaBlazor();
                }
            });
        }

        protected override WindowSettings ConfigureWindowSettings(HostWindowBuilder opts)
        {
            var win = opts.UseDefaultWindow();
            win.ExtendsContentIntoTitleBar = true;
            win.Resizable = true;
            win.SystemBackdropType = SystemBackdropType.BlurBehind;

            return win;
        }
    }
}
