using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Tinvo;
using Tinvo.Abstractions;
using Tinvo.Application;
using Tinvo.Application.AIAssistant;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.DataStorage;
using Tinvo.Application.DB;
using Tinvo.Application.Provider;
using Tinvo.Provider.Baidu;
using Tinvo.Provider.MCP;
using Tinvo.Provider.OpenAI;
using Tinvo.Provider.XunFei;
using Tinvo.Service;
using Tinvo.Service.Chat;
using Tinvo.Service.KBS;
using static Tinvo.MiniblinkNative;

namespace Tinvo;

class Program
{
    public static MiniblinkWebViewManager? m = null;

    public static Uri BaseUri = new Uri("https://localhost/");

    public static bool isStart = false;

    public static ILogger logger;

    public static IntPtr webWindow;

    public static IntPtr mainFrameId;

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == "miniblink")
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64 && OperatingSystem.IsWindows())
                return NativeLibrary.Load("runtimes\\win-x64\\native\\miniblink.dll", assembly, searchPath);
            else if (RuntimeInformation.ProcessArchitecture == Architecture.X86 && OperatingSystem.IsWindows())
                return NativeLibrary.Load("runtimes\\win-x86\\native\\miniblink.dll", assembly, searchPath);
            else if (RuntimeInformation.ProcessArchitecture == Architecture.X64 && OperatingSystem.IsLinux())
                return NativeLibrary.Load("runtimes\\linux-x64\\native\\miniblink.so", assembly, searchPath);
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && OperatingSystem.IsLinux())
                return NativeLibrary.Load("runtimes\\linux-arm64\\native\\miniblink.so", assembly, searchPath);
        }

        return IntPtr.Zero;
    }

    static void Main()
    {
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);

        Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Tinvo", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();
        logger = Log.ForContext<Program>();

        MiniblinkNative.mbInit(IntPtr.Zero);

        webWindow = MiniblinkNative.mbCreateWebWindow(MiniblinkNative.mbWindowType.WKE_WINDOW_TYPE_POPUP, IntPtr.Zero, 0, 0, 800, 600);
        mainFrameId = MiniblinkNative.mbWebFrameGetMainFrame(webWindow);

        MiniblinkNative.mbSetCspCheckEnable(webWindow, false);

        MiniblinkNative.mbOnClose(webWindow, static (IntPtr webView, IntPtr param, IntPtr unuse) =>
        {
            Environment.Exit(0);
            return true;
        }, IntPtr.Zero);

        MiniblinkNative.mbOnJsQuery(webWindow, static (IntPtr webView, IntPtr param, IntPtr es, long queryId, int customMsg, IntPtr request) =>
        {
            if (customMsg != 0)
                return;
            var str = request.UTF8PtrToStr();
            Program.logger.Debug($"MiniblinkPostMessage: {str}");
            m!.MessageReceived(str!);
        }, IntPtr.Zero);

        MiniblinkNative.mbOnConsole(webWindow, static (IntPtr webView, IntPtr param, mbConsoleLevel level, IntPtr message, IntPtr sourceName, uint sourceLine, IntPtr stackTrace) =>
        {
            var messageStr = message.UTF8PtrToStr();
            var sourceNameStr = sourceName.UTF8PtrToStr();
            Program.logger.Debug($"wkeOnConsole({level})({sourceNameStr})({sourceLine}): {messageStr}");
        }, IntPtr.Zero);

        MiniblinkNative.mbOnLoadUrlBegin(webWindow, static (IntPtr webView, IntPtr param, IntPtr url, IntPtr job) =>
        {
            var urlStr = url.UTF8PtrToStr();
            Program.logger.Debug($"wkeOnLoadUrlBegin: {urlStr}");
            var ruri = new Uri(urlStr);
            if (ruri.Host != "localhost")
                return false;

            var allowFallbackOnHostPage = BaseUri.IsBaseOfPage(urlStr);
            var requestWrapper = new WebResourceRequest
            {
                RequestUri = urlStr,
                AllowFallbackOnHostPage = allowFallbackOnHostPage,
            };

            var bRet = m!.PlatformWebViewResourceRequested(requestWrapper, out var response);

            if (!bRet || response is null)
            {
                Program.logger.Debug($"wkeOnLoadUrlBegin(404): {urlStr}");
                return false;
            }

            var headerString = response.Headers[QueryStringHelper.ContentTypeKey];
            Program.logger.Debug($"wkeOnLoadUrlBegin_headerString: {headerString}");

            using var ms = new MemoryStream();
            response.Content.CopyTo(ms);
            var requestData = ms.ToArray();

            MiniblinkNative.mbNetSetMIMEType(job, headerString.StrToUtf8Ptr());
            MiniblinkNative.mbNetSetData(job, requestData, requestData.Length);

            Program.logger.Debug($"wkeOnLoadUrlBegin_wkeNetSetData: {requestData.Length}");
            return true;
        }, IntPtr.Zero);

        MiniblinkNative.mbMoveToCenter(webWindow);
        MiniblinkNative.mbShowWindow(webWindow, true);

        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

        services.AddSingleton<IPlatform>(s =>
        {
            return new Platform()
            {
                Type = PlatformType.Linux
            };
        });

        services.AddSingleton<ICryptographyService, MachineFingerprintCryptographyService>();

        services.AddSingleton<IDataStorageServiceFactory>(s =>
        {
            return new DataStorageServiceFactory(new FileStorageService(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tinvo")), s.GetRequiredService<ICryptographyService>());
        });

        services.AddSingleton<DBData<AssistantEntity>>();
        services.AddSingleton<AIAssistantService>();

        services.AddScoped<IChatService, LocalChatService>();
        services.AddScoped<IKBSService, LocalKBSService>();

        services.AddProviderRegisterer()
                .RegistererBaiduProvider()
                .RegistererOpenAIProvider()
                .RegistererXunFeiProvider()
                .RegistererMCPProvider();

        services.AddSingleton<ProviderService>();

        services.AddMudServices();

        services.AddMasaBlazor();

        services.AddBlazorWebView();

        services.AddSingleton<JSComponentConfigurationStore>();

        services.AddSingleton<TDispatcher>(provider => new TDispatcher());

        services.AddSingleton<IJSComponentConfiguration>(provider => new JsComponentConfigration(provider.GetRequiredService<JSComponentConfigurationStore>()));

        var sb = services.BuildServiceProvider();

        var appRootDir = AppContext.BaseDirectory;
        var hostPageFullPath = Path.GetFullPath(Path.Combine(appRootDir, "wwwroot/index.html"));
        var contentRootDirFullPath = Path.GetDirectoryName(hostPageFullPath)!;
        var hostPageRelativePath = Path.GetRelativePath(contentRootDirFullPath, hostPageFullPath);
        var b = new BlazorWebViewHandlerProvider();
        var f = b.CreateFileProvider(typeof(Main).Assembly, contentRootDirFullPath);
        m = new MiniblinkWebViewManager(webWindow, sb, sb.GetRequiredService<TDispatcher>(), BaseUri, f, sb.GetRequiredService<JSComponentConfigurationStore>(), hostPageRelativePath);
        m.AddRootComponentAsync(typeof(Main), "#app", ParameterView.Empty).Wait();

        Program.logger.Debug("Start...");
        m.Navigate("/");

        isStart = true;

        var t = new Thread(() =>
        {
            while (true)
            {
                if (isStart && m != null)
                {
                    if (m.MessageQueue.TryDequeue(out var script))
                    {
                        MiniblinkNative.mbRunJs(m.WebView, mainFrameId, script.StrToUtf8Ptr(), false, static (IntPtr webView, IntPtr param, IntPtr es, long v) =>
                        {

                        }, IntPtr.Zero, IntPtr.Zero);
                    }
                }
            }
        });
        t.Start();

        MiniblinkNative.mbRunMessageLoop();
    }
}
