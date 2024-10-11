using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TrustInnova
{
    public class MiniblinkWebViewManager : WebViewManager
    {
        private readonly IntPtr _webView;
        private readonly Uri _appBaseUri;
        private readonly ILogger _logger;

        public ConcurrentQueue<string> MessageQueue = new ConcurrentQueue<string>();
        public IntPtr WebView => _webView;

        public MiniblinkWebViewManager(IntPtr webView, IServiceProvider provider, Dispatcher dispatcher, Uri appBaseUri, IFileProvider fileProvider, JSComponentConfigurationStore jsComponents, string hostPageRelativePath) : base(provider, dispatcher, appBaseUri, fileProvider, jsComponents, hostPageRelativePath)
        {
            _webView = webView;
            _appBaseUri = appBaseUri;
            _logger = Log.ForContext<MiniblinkWebViewManager>();
        }

        protected override void NavigateCore(Uri absoluteUri)
        {
            var u = absoluteUri.ToString();
            _logger.Debug($"MiniblinkWebViewManager: {u}");
            MiniblinkNative.mbLoadURL(_webView, absoluteUri.AbsoluteUri.StrToUtf8Ptr());
        }

        protected override void SendMessage(string message)
        {
            try
            {
                _logger.Debug($"MiniblinkWebViewManager_SendMessage: {message}");
                if (string.IsNullOrWhiteSpace(message))
                    return;

                var messageJSStringLiteral = HttpUtility.JavaScriptStringEncode(message);
                var script = $"window.__dispatchMessageCallback((\"{messageJSStringLiteral}\"))";

                MessageQueue.Enqueue(script);
            }
            catch (Exception ex)
            {
                _logger.Debug($"MiniblinkWebViewManager_SendMessage(ERROR): {ex.Message}");
            }
        }

        public bool PlatformWebViewResourceRequested(WebResourceRequest request, out WebResourceResponse? response)
        {
            response = default;
            if (request is null)
                return false;

            var requestUri = QueryStringHelper.RemovePossibleQueryString(request.RequestUri);
            if (!TryGetResponseContent(requestUri, request.AllowFallbackOnHostPage, out var statusCode, out var statusMessage, out var content, out var headers))
                return false;

            //StaticContentHotReloadManager.TryReplaceResponseContent(_contentRootDirRelativePath, requestUri, ref statusCode, ref content, headers);
            var contentStream = new AutoCloseOnReadCompleteStream(content);
            response = new WebResourceResponse
            {
                StatusCode = statusCode,
                StatusMessage = statusMessage,
                Content = contentStream,
                Headers = headers,
            };

            return true;
        }

        public void MessageReceived(string message)
        {
            _logger.Debug($"MessageReceived({_appBaseUri}): {message}");
            MessageReceived(_appBaseUri, message);
        }
    }

    public class QueryStringHelper
    {
        public static string ContentTypeKey = "Content-Type";

        public static string RemovePossibleQueryString(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            var indexOfQueryString = url!.IndexOf("?", 0, url.Length, StringComparison.Ordinal);
            return (indexOfQueryString == -1) ? url : url.Substring(0, indexOfQueryString);
        }
    }

    public class WebResourceRequest
    {
        public required string RequestUri { get; set; }

        public required bool AllowFallbackOnHostPage { get; set; }
    }

    public class WebResourceResponse
    {
        public required int StatusCode { get; set; }
        public required string StatusMessage { get; set; }
        public required Stream Content { get; set; }
        public required IDictionary<string, string> Headers { get; set; }
    }

    internal class AutoCloseOnReadCompleteStream : Stream
    {
        private readonly Stream _baseStream;

        public AutoCloseOnReadCompleteStream(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position { get => _baseStream.Position; set => _baseStream.Position = value; }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = _baseStream.Read(buffer, offset, count);

            // Stream.Read only returns 0 when it has reached the end of stream
            // and no further bytes are expected. Otherwise it blocks until
            // one or more (and at most count) bytes can be read.
            if (bytesRead == 0)
                _baseStream.Close();

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
    }



    public class JsComponentConfigration : IJSComponentConfiguration
    {
        public JsComponentConfigration(JSComponentConfigurationStore jSComponentConfigurationStore)
        {
            JSComponents = jSComponentConfigurationStore;
        }

        public JSComponentConfigurationStore JSComponents { get; init; }
    }

    public class BlazorWebViewHandlerProvider
    {
        public IFileProvider CreateFileProvider(Assembly? assembly, string contentRootDirFullPath)
        {
            if (Directory.Exists(contentRootDirFullPath))
                return new PhysicalFileProvider(contentRootDirFullPath);
            else
                return new NullFileProvider();
        }
    }

    public class TDispatcher : Dispatcher
    {
        public override bool CheckAccess()
        {
            return true;
        }

        public override Task InvokeAsync(Action workItem)
        {
            try
            {
                workItem();
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        public override async Task InvokeAsync(Func<Task> workItem)
        {
            await workItem();
        }

        public override async Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            return workItem();
        }

        public override async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            return await workItem();
        }
    }
}
