using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo
{
    /// <summary>
    /// C# P/Invoke declarations for the miniblink library (mb.dll).
    /// This static class contains all the necessary structs, enums, delegates,
    /// and DllImport statements to interact with the native miniblink API.
    ///
    /// License: Apache-2.0
    /// Project Home: https://github.com/weolar/miniblink49
    /// </summary>
    public static class MiniblinkNative
    {
        /// <summary>
        /// The name of the miniblink DLL.
        /// Ensure the correct 32-bit (mb_x32.dll) or 64-bit (mb_x64.dll) version of the DLL
        /// is available in the application's search path and rename it to "mb.dll",
        /// or change the constant value.
        /// </summary>
        private const string DllName = "miniblink";

        private const CallingConvention CallConv = CallingConvention.StdCall;

        #region Constants and Opaque Types

        public const int NULL_WEBVIEW = 0;
        public const int kMbVersion = 20210809;
        public const int kMbMaxVersion = 20600319;

        // Opaque types are mapped to IntPtr
        // mbWebView, mbWebFrameHandle, mbNetJob, mbStringPtr, mbJsExecState, etc.
        // mbJsValue is a long (long long)

        #endregion

        #region Enumerations

        [Flags]
        public enum mbMouseFlags
        {
            MB_LBUTTON = 0x01,
            MB_RBUTTON = 0x02,
            MB_SHIFT = 0x04,
            MB_CONTROL = 0x08,
            MB_MBUTTON = 0x10,
        }

        [Flags]
        public enum mbKeyFlags
        {
            MB_EXTENDED = 0x0100,
            MB_REPEAT = 0x4000,
        }

        public enum mbMouseMsg
        {
            MB_MSG_MOUSEMOVE = 0x0200,
            MB_MSG_LBUTTONDOWN = 0x0201,
            MB_MSG_LBUTTONUP = 0x0202,
            MB_MSG_LBUTTONDBLCLK = 0x0203,
            MB_MSG_RBUTTONDOWN = 0x0204,
            MB_MSG_RBUTTONUP = 0x0205,
            MB_MSG_RBUTTONDBLCLK = 0x0206,
            MB_MSG_MBUTTONDOWN = 0x0207,
            MB_MSG_MBUTTONUP = 0x0208,
            MB_MSG_MBUTTONDBLCLK = 0x0209,
            MB_MSG_MOUSEWHEEL = 0x020A,
        }

        public enum mbProxyType
        {
            MB_PROXY_NONE,
            MB_PROXY_HTTP,
            MB_PROXY_SOCKS4,
            MB_PROXY_SOCKS4A,
            MB_PROXY_SOCKS5,
            MB_PROXY_SOCKS5HOSTNAME
        }

        [Flags]
        public enum mbSettingMask
        {
            MB_SETTING_PROXY = 1,
            MB_ENABLE_NODEJS = 1 << 3,
            MB_ENABLE_DISABLE_H5VIDEO = 1 << 4,
            MB_ENABLE_DISABLE_PDFVIEW = 1 << 5,
            MB_ENABLE_DISABLE_CC = 1 << 6,
            MB_ENABLE_ENABLE_EGLGLES2 = 1 << 7,
            MB_ENABLE_ENABLE_SWIFTSHAER = 1 << 8,
        }

        public enum mbCookieCommand
        {
            mbCookieCommandClearAllCookies,
            mbCookieCommandClearSessionCookies,
            mbCookieCommandFlushCookiesToFile,
            mbCookieCommandReloadCookiesFromFile,
        }

        public enum mbNavigationType
        {
            MB_NAVIGATION_TYPE_LINKCLICK,
            MB_NAVIGATION_TYPE_FORMSUBMITTE,
            MB_NAVIGATION_TYPE_BACKFORWARD,
            MB_NAVIGATION_TYPE_RELOAD,
            MB_NAVIGATION_TYPE_FORMRESUBMITT,
            MB_NAVIGATION_TYPE_OTHER
        }

        public enum mbCursorInfoType
        {
            kMbCursorInfoPointer, kMbCursorInfoCross, kMbCursorInfoHand, kMbCursorInfoIBeam,
            kMbCursorInfoWait, kMbCursorInfoHelp, kMbCursorInfoEastResize, kMbCursorInfoNorthResize,
            kMbCursorInfoNorthEastResize, kMbCursorInfoNorthWestResize, kMbCursorInfoSouthResize,
            kMbCursorInfoSouthEastResize, kMbCursorInfoSouthWestResize, kMbCursorInfoWestResize,
            kMbCursorInfoNorthSouthResize, kMbCursorInfoEastWestResize, kMbCursorInfoNorthEastSouthWestResize,
            kMbCursorInfoNorthWestSouthEastResize, kMbCursorInfoColumnResize, kMbCursorInfoRowResize,
            kMbCursorInfoMiddlePanning, kMbCursorInfoEastPanning, kMbCursorInfoNorthPanning,
            kMbCursorInfoNorthEastPanning, kMbCursorInfoNorthWestPanning, kMbCursorInfoSouthPanning,
            kMbCursorInfoSouthEastPanning, kMbCursorInfoSouthWestPanning, kMbCursorInfoWestPanning,
            kMbCursorInfoMove, kMbCursorInfoVerticalText, kMbCursorInfoCell, kMbCursorInfoContextMenu,
            kMbCursorInfoAlias, kMbCursorInfoProgress, kMbCursorInfoNoDrop, kMbCursorInfoCopy,
            kMbCursorInfoNone, kMbCursorInfoNotAllowed, kMbCursorInfoZoomIn, kMbCursorInfoZoomOut,
            kMbCursorInfoGrab, kMbCursorInfoGrabbing, kMbCursorInfoCustom
        }

        public enum mbStorageType
        {
            StorageTypeString,
            StorageTypeFilename,
            StorageTypeBinaryData,
            StorageTypeFileSystemFile,
        }

        [Flags]
        public enum mbWebDragOperation : uint
        {
            mbWebDragOperationNone = 0,
            mbWebDragOperationCopy = 1,
            mbWebDragOperationLink = 2,
            mbWebDragOperationGeneric = 4,
            mbWebDragOperationPrivate = 8,
            mbWebDragOperationMove = 16,
            mbWebDragOperationDelete = 32,
            mbWebDragOperationEvery = 0xffffffff
        }

        public enum mbResourceType
        {
            MB_RESOURCE_TYPE_MAIN_FRAME, MB_RESOURCE_TYPE_SUB_FRAME, MB_RESOURCE_TYPE_STYLESHEET,
            MB_RESOURCE_TYPE_SCRIPT, MB_RESOURCE_TYPE_IMAGE, MB_RESOURCE_TYPE_FONT_RESOURCE,
            MB_RESOURCE_TYPE_SUB_RESOURCE, MB_RESOURCE_TYPE_OBJECT, MB_RESOURCE_TYPE_MEDIA,
            MB_RESOURCE_TYPE_WORKER, MB_RESOURCE_TYPE_SHARED_WORKER, MB_RESOURCE_TYPE_PREFETCH,
            MB_RESOURCE_TYPE_FAVICON, MB_RESOURCE_TYPE_XHR, MB_RESOURCE_TYPE_PING,
            MB_RESOURCE_TYPE_SERVICE_WORKER, MB_RESOURCE_TYPE_LAST_TYPE
        }

        public enum mbRequestType
        {
            kMbRequestTypeInvalidation,
            kMbRequestTypeGet,
            kMbRequestTypePost,
            kMbRequestTypePut,
        }

        [Flags]
        public enum mbMenuItemId
        {
            kMbMenuSelectedAllId = 1 << 1, kMbMenuSelectedTextId = 1 << 2, kMbMenuUndoId = 1 << 3,
            kMbMenuCopyImageId = 1 << 4, kMbMenuInspectElementAtId = 1 << 5, kMbMenuCutId = 1 << 6,
            kMbMenuPasteId = 1 << 7, kMbMenuPrintId = 1 << 8, kMbMenuGoForwardId = 1 << 9,
            kMbMenuGoBackId = 1 << 10, kMbMenuReloadId = 1 << 11, kMbMenuSaveImageId = 1 << 12,
        }

        public enum mbJsType
        {
            kMbJsTypeNumber = 0, kMbJsTypeString = 1, kMbJsTypeBool = 2,
            kMbJsTypeUndefined = 5, kMbJsTypeNull = 7, kMbJsTypeV8Value = 8,
            kMbJsTypeFrame = 9,
        }

        public enum mbImageFormat
        {
            kMbImageFormatPng = 0,
            kMbImageFormatJpg = 1,
            kMbImageFormatBmp = 2,
        }

        public enum mbLoadingResult
        {
            MB_LOADING_SUCCEEDED,
            MB_LOADING_FAILED,
            MB_LOADING_CANCELED
        }

        public enum mbConsoleLevel
        {
            mbLevelDebug = 4, mbLevelLog = 1, mbLevelInfo = 5,
            mbLevelWarning = 2, mbLevelError = 3, mbLevelRevokedError = 6,
            mbLevelLast = mbLevelRevokedError
        }

        public enum MbAsynRequestState
        {
            kMbAsynRequestStateOk = 0,
            kMbAsynRequestStateFail = 1,
        }

        public enum mbDownloadOpt
        {
            kMbDownloadOptCancel,
            kMbDownloadOptCacheData,
        }

        [Flags]
        public enum mbDialogProperties
        {
            kMbDialogPropertiesOpenFile = 1 << 1, kMbDialogPropertiesOpenDirectory = 1 << 2,
            kMbDialogPropertiesMultiSelections = 1 << 3, kMbDialogPropertiesShowHiddenFiles = 1 << 4,
            kMbDialogPropertiesCreateDirectory = 1 << 5, kMbDialogPropertiesPromptToCreate = 1 << 6,
            kMbDialogPropertiesNoResolveAliases = 1 << 7, kMbDialogPropertiesTreatPackageAsDirectory = 1 << 8,
            kMbDialogPropertiesDontAddToRecent = 1 << 9,
        }

        public enum mbHttBodyElementType
        {
            mbHttBodyElementTypeData,
            mbHttBodyElementTypeFile,
        }

        public enum mbWindowType
        {
            MB_WINDOW_TYPE_POPUP,
            MB_WINDOW_TYPE_TRANSPARENT,
            MB_WINDOW_TYPE_CONTROL
        }

        [Flags]
        public enum mbWindowInfo
        {
            MB_WINDOW_INFO_SHARTD_TEXTURE_ENABLE = 1 << 16,
        }

        public enum mbPrintintStep
        {
            kPrintintStepStart,
            kPrintintStepPreview,
            kPrintintStepPrinting,
        }

        public enum mbViewLoadType
        {
            MB_DID_START_LOADING, MB_DID_STOP_LOADING, MB_DID_NAVIGATE,
            MB_DID_NAVIGATE_IN_PAGE, MB_DID_GET_RESPONSE_DETAILS,
            MB_DID_GET_REDIRECT_REQUEST, MB_DID_POST_REQUEST,
        }

        #endregion

        #region Delegates (Callbacks)

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnBlinkThreadInitCallback(IntPtr param);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbCookieVisitor(IntPtr @params,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string domain,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
            int secure, int httpOnly, ref int expires);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate IntPtr mbStringPtr(IntPtr webView, IntPtr param, IntPtr channel, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.I1)] ref bool needHook);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbOnConnectedCallback(IntPtr webView, IntPtr param, IntPtr channel);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate IntPtr mbOnReceiveCallback(IntPtr webView, IntPtr param, IntPtr channel, int opCode, IntPtr buf, IntPtr len, [MarshalAs(UnmanagedType.I1)] ref bool isContinue);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate IntPtr mbOnSendCallback(IntPtr webView, IntPtr param, IntPtr channel, int opCode, IntPtr buf, IntPtr len, [MarshalAs(UnmanagedType.I1)] ref bool isContinue);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnErrorCallback(IntPtr webView, IntPtr param, IntPtr channel);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnGetPdfPageDataCallback(IntPtr webView, IntPtr param, IntPtr data, IntPtr size);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbRunJsCallback(IntPtr webView, IntPtr param, IntPtr es, long v);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbJsQueryCallback(IntPtr webView, IntPtr param, IntPtr es, long queryId, int customMsg, [MarshalAs(UnmanagedType.LPUTF8Str)] string request);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbJsQueryExCallback(IntPtr webView, IntPtr param, IntPtr es, ref long val, int count);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbTitleChangedCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbMouseOverUrlChangedCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbURLChangedCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.I1)] bool canGoBack, [MarshalAs(UnmanagedType.I1)] bool canGoForward);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbURLChangedCallback2(IntPtr webView, IntPtr param, IntPtr frameId, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbPaintUpdatedCallback(IntPtr webView, IntPtr param, IntPtr hdc, int x, int y, int cx, int cy);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbAcceleratedPaintCallback(IntPtr webView, IntPtr param, int type, IntPtr dirytRects, IntPtr dirytRectsSize, IntPtr sharedHandle);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbPaintBitUpdatedCallback(IntPtr webView, IntPtr param, IntPtr buffer, ref mbRect r, int width, int height);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbAlertBoxCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbConfirmBoxCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate IntPtr mbPromptBoxCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string msg, [MarshalAs(UnmanagedType.LPUTF8Str)] string defaultResult, [MarshalAs(UnmanagedType.I1)] ref bool result);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbNavigationCallback(IntPtr webView, IntPtr param, mbNavigationType navigationType, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate IntPtr mbCreateViewCallback(IntPtr webView, IntPtr param, mbNavigationType navigationType, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr windowFeatures);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbDocumentReadyCallback(IntPtr webView, IntPtr param, IntPtr frameId);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbLoadUrlFinishCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr job, int len);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbLoadUrlHeadersReceivedCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr job);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbCloseCallback(IntPtr webView, IntPtr param, IntPtr unuse);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbDestroyCallback(IntPtr webView, IntPtr param, IntPtr unuse);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnShowDevtoolsCallback(IntPtr webView, IntPtr param);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbDidCreateScriptContextCallback(IntPtr webView, IntPtr param, IntPtr frameId, IntPtr context, int extensionGroup, int worldId);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbGetPluginListCallback([MarshalAs(UnmanagedType.I1)] bool refresh, IntPtr pluginListBuilder, IntPtr param);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbNetResponseCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr job);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbThreadCallback(IntPtr param1, IntPtr param2);

        [UnmanagedFunctionPointer(CallConv, CharSet = CharSet.Unicode)]
        public delegate void mbNodeOnCreateProcessCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPWStr)] string applicationPath, [MarshalAs(UnmanagedType.LPWStr)] string arguments, IntPtr startup);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbLoadingFinishCallback(IntPtr webView, IntPtr param, IntPtr frameId, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, mbLoadingResult result, [MarshalAs(UnmanagedType.LPUTF8Str)] string failedReason);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbDownloadCallback(IntPtr webView, IntPtr param, IntPtr frameId, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr downloadJob);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbConsoleCallback(IntPtr webView, IntPtr param, mbConsoleLevel level, [MarshalAs(UnmanagedType.LPUTF8Str)] string message, [MarshalAs(UnmanagedType.LPUTF8Str)] string sourceName, uint sourceLine, [MarshalAs(UnmanagedType.LPUTF8Str)] string stackTrace);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnCallUiThread(IntPtr webView, IntPtr paramOnInThread);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbCallUiThread(IntPtr webView, mbOnCallUiThread func, IntPtr param);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbLoadUrlBeginCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr job);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbLoadUrlEndCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr job, IntPtr buf, int len);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbLoadUrlFailCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr job);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbWillReleaseScriptContextCallback(IntPtr webView, IntPtr param, IntPtr frameId, IntPtr context, int worldId);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbNetGetFaviconCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr buf);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbCanGoBackForwardCallback(IntPtr webView, IntPtr param, MbAsynRequestState state, [MarshalAs(UnmanagedType.I1)] bool b);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbGetCookieCallback(IntPtr webView, IntPtr param, MbAsynRequestState state, [MarshalAs(UnmanagedType.LPUTF8Str)] string cookie);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbGetSourceCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string mhtml);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbGetContentAsMarkupCallback(IntPtr webView, IntPtr param, [MarshalAs(UnmanagedType.LPUTF8Str)] string content, IntPtr size);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnUrlRequestWillRedirectCallback(IntPtr webView, IntPtr param, IntPtr oldRequest, IntPtr request, IntPtr redirectResponse);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnUrlRequestDidReceiveResponseCallback(IntPtr webView, IntPtr param, IntPtr request, IntPtr response);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnUrlRequestDidReceiveDataCallback(IntPtr webView, IntPtr param, IntPtr request, [MarshalAs(UnmanagedType.LPStr, SizeParamIndex = 4)] byte[] data, int dataLength);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnUrlRequestDidFailCallback(IntPtr webView, IntPtr param, IntPtr request, [MarshalAs(UnmanagedType.LPUTF8Str)] string error);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnUrlRequestDidFinishLoadingCallback(IntPtr webView, IntPtr param, IntPtr request, double finishTime);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbNetJobDataRecvCallback(IntPtr ptr, IntPtr job, IntPtr data, int length);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbNetJobDataFinishCallback(IntPtr ptr, IntPtr job, mbLoadingResult result);

        [UnmanagedFunctionPointer(CallConv, CharSet = CharSet.Unicode)]
        public delegate void mbPopupDialogSaveNameCallback(IntPtr ptr, [MarshalAs(UnmanagedType.LPWStr)] string filePath);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate IntPtr mbNetBeginSaveCallback(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string filePath, [MarshalAs(UnmanagedType.I1)] bool isPathExists);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate mbDownloadOpt mbDownloadInBlinkThreadCallback(IntPtr webView, IntPtr param, IntPtr expectedContentLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.LPUTF8Str)] string mime, [MarshalAs(UnmanagedType.LPUTF8Str)] string disposition, IntPtr job, ref mbNetJobDataBind dataBind);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbPrintPdfDataCallback(IntPtr webview, IntPtr param, IntPtr datas);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbPrintBitmapCallback(IntPtr webview, IntPtr param, IntPtr data, IntPtr size);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbOnScreenshot(IntPtr webView, IntPtr param, IntPtr data, IntPtr size);

        [UnmanagedFunctionPointer(CallConv)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbWindowClosingCallback(IntPtr webview, IntPtr param);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbWindowDestroyCallback(IntPtr webview, IntPtr param);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbDraggableRegionsChangedCallback(IntPtr webview, IntPtr param, IntPtr rects, int rectCount);

        [UnmanagedFunctionPointer(CallConv, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool mbPrintingCallback(IntPtr webview, IntPtr param, mbPrintintStep step, IntPtr hDC, IntPtr settings, int pageCount);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate IntPtr mbImageBufferToDataURLCallback(IntPtr webView, IntPtr param, IntPtr data, IntPtr size);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void mbNetViewLoadInfoCallback(IntPtr webView, IntPtr param, mbViewLoadType type, IntPtr info);

        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct mbRect
        {
            public int x;
            public int y;
            public int w;
            public int h;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbPoint
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbSize
        {
            public int w;
            public int h;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct mbProxy
        {
            public mbProxyType type;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string hostname;
            public ushort port;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string username;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string password;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct mbSettings
        {
            public mbProxy proxy;
            public uint mask;
            public IntPtr blinkThreadInitCallback; // mbOnBlinkThreadInitCallback
            public IntPtr blinkThreadInitCallbackParam;
            public IntPtr version;
            public IntPtr mainDllPath; // WCHAR*
            public IntPtr mainDllHandle; // HMODULE
            public IntPtr config; // const char*
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbViewSettings
        {
            public int size;
            public uint bgColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbWindowFeatures
        {
            public int x;
            public int y;
            public int width;
            public int height;
            [MarshalAs(UnmanagedType.I1)]
            public bool menuBarVisible;
            [MarshalAs(UnmanagedType.I1)]
            public bool statusBarVisible;
            [MarshalAs(UnmanagedType.I1)]
            public bool toolBarVisible;
            [MarshalAs(UnmanagedType.I1)]
            public bool locationBarVisible;
            [MarshalAs(UnmanagedType.I1)]
            public bool scrollbarsVisible;
            [MarshalAs(UnmanagedType.I1)]
            public bool resizable;
            [MarshalAs(UnmanagedType.I1)]
            public bool fullscreen;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbPrintSettings
        {
            public int structSize;
            public int dpi;
            public int width;
            public int height;
            public int marginTop;
            public int marginBottom;
            public int marginLeft;
            public int marginRight;
            [MarshalAs(UnmanagedType.I1)]
            public bool isPrintPageHeadAndFooter;
            [MarshalAs(UnmanagedType.I1)]
            public bool isPrintBackgroud;
            [MarshalAs(UnmanagedType.I1)]
            public bool isLandscape;
            [MarshalAs(UnmanagedType.I1)]
            public bool isPrintToMultiPage;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbMemBuf
        {
            public int unuse;
            public IntPtr data;
            public IntPtr length;
        }

        // NOTE: mbWebDragData is complex and requires careful manual marshalling
        [StructLayout(LayoutKind.Sequential)]
        public struct mbWebDragData
        {
            public IntPtr m_itemList; // struct Item*
            public int m_itemListLength;
            public int m_modifierKeyState;
            public IntPtr m_filesystemId; // mbMemBuf*
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbSlist
        {
            public IntPtr data; // char*
            public IntPtr next; // struct _mbSlist*
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbWebsocketHookCallbacks
        {
            public IntPtr onWillConnect; // mbStringPtr(MB_CALL_TYPE* )(...) -> delegate
            public IntPtr onConnected; // BOOL(MB_CALL_TYPE* )(...) -> delegate
            public IntPtr onReceive; // mbStringPtr(MB_CALL_TYPE* )(...) -> delegate
            public IntPtr onSend; // mbStringPtr(MB_CALL_TYPE* )(...) -> delegate
            public IntPtr onError; // void(MB_CALL_TYPE* )(...) -> delegate
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbUrlRequestCallbacks
        {
            public IntPtr willRedirectCallback;
            public IntPtr didReceiveResponseCallback;
            public IntPtr didReceiveDataCallback;
            public IntPtr didFailCallback;
            public IntPtr didFinishLoadingCallback;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbNetJobDataBind
        {
            public IntPtr param;
            public IntPtr recvCallback; // mbNetJobDataRecvCallback
            public IntPtr finishCallback; // mbNetJobDataFinishCallback
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbDownloadBind
        {
            public IntPtr param;
            public IntPtr recvCallback; // mbNetJobDataRecvCallback
            public IntPtr finishCallback; // mbNetJobDataFinishCallback
            public IntPtr saveNameCallback; // mbPopupDialogSaveNameCallback
            public IntPtr beginSaveCallback; // mbNetBeginSaveCallback
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct mbFileFilter
        {
            public IntPtr name; // const utf8*
            public IntPtr extensions; // const utf8*
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct mbDialogOptions
        {
            public int magic; // 'mbdo'
            public IntPtr title; // const utf8*
            public IntPtr defaultPath; // const utf8*
            public IntPtr buttonLabel; // const utf8*
            public IntPtr filters; // mbFileFilter*
            public int filtersCount;
            public mbDialogProperties prop;
            public IntPtr message; // const utf8*
            [MarshalAs(UnmanagedType.I1)]
            public bool securityScopedBookmarks;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbDownloadOptions
        {
            public int magic; // 'mbdo'
            [MarshalAs(UnmanagedType.I1)]
            public bool saveAsPathAndName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbPdfDatas
        {
            public int count;
            public IntPtr sizes; // size_t*
            public IntPtr datas; // const void**
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbScreenshotSettings
        {
            public int structSize;
            public int width;
            public int height;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbPostBodyElement
        {
            public int size;
            public mbHttBodyElementType type;
            public IntPtr data; // mbMemBuf*
            public IntPtr filePath; // mbStringPtr
            public long fileStart; // __int64
            public long fileLength; // __int64
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbPostBodyElements
        {
            public int size;
            public IntPtr element; // mbPostBodyElement**
            public IntPtr elementSize;
            [MarshalAs(UnmanagedType.I1)]
            public bool isDirty;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbDraggableRegion
        {
            public RECT bounds;
            [MarshalAs(UnmanagedType.I1)]
            public bool draggable;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbPrintintSettings
        {
            public int dpi;
            public int width;
            public int height;
            public float scale;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbDefaultPrinterSettings
        {
            public int structSize;
            [MarshalAs(UnmanagedType.I1)]
            public bool isLandscape;
            [MarshalAs(UnmanagedType.I1)]
            public bool isPrintHeadFooter;
            [MarshalAs(UnmanagedType.I1)]
            public bool isPrintBackgroud;
            public int edgeDistanceLeft;
            public int edgeDistanceTop;
            public int edgeDistanceRight;
            public int edgeDistanceBottom;
            public int copies;
            public int paperType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbWillSendRequestInfo
        {
            public IntPtr url; // mbStringPtr
            public IntPtr newUrl; // mbStringPtr
            public mbResourceType resourceType;
            public int httpResponseCode;
            public IntPtr method; // mbStringPtr
            public IntPtr referrer; // mbStringPtr
            public IntPtr headers; // void*
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mbViewLoadCallbackInfo
        {
            public int size;
            public IntPtr frame; // mbWebFrameHandle
            public IntPtr willSendRequestInfo; // mbWillSendRequestInfo*
            public IntPtr url; // const char*
            public IntPtr postBody; // mbPostBodyElements*
            public IntPtr job; // mbNetJob
        }
        #endregion

        #region API Functions

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbInit")]
        public static extern void Init(IntPtr settings);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUninit")]
        public static extern void Uninit();

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCreateInitSettings")]
        public static extern IntPtr CreateInitSettings();

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbSetInitSettings")]
        public static extern void SetInitSettings(IntPtr settings, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCreateWebView")]
        public static extern IntPtr CreateWebView();

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbDestroyWebView")]
        public static extern void DestroyWebView(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCreateWebWindow")]
        public static extern IntPtr CreateWebWindow(mbWindowType type, IntPtr parent, int x, int y, int width, int height);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCreateWebWindowEx")]
        public static extern IntPtr CreateWebWindowEx(mbWindowType type, IntPtr parent, int x, int y, int width, int height, ref mbViewSettings settings);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCreateWebCustomWindow")]
        public static extern IntPtr CreateWebCustomWindow(IntPtr parent, uint style, uint styleEx, int x, int y, int width, int height);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbMoveWindow")]
        public static extern void MoveWindow(IntPtr webview, int x, int y, int w, int h);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbMoveToCenter")]
        public static extern void MoveToCenter(IntPtr webview);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetAutoDrawToHwnd")]
        public static extern void SetAutoDrawToHwnd(IntPtr webview, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetCaretRect")]
        public static extern void GetCaretRect(IntPtr webviewHandle, out mbRect r);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetAudioMuted")]
        public static extern void SetAudioMuted(IntPtr webview, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbIsAudioMuted")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsAudioMuted(IntPtr webview);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbCreateString")]
        public static extern IntPtr CreateString([MarshalAs(UnmanagedType.LPUTF8Str)] string str, IntPtr length);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbCreateStringWithCopy")]
        public static extern IntPtr CreateStringWithCopy([MarshalAs(UnmanagedType.LPUTF8Str)] string str, IntPtr length);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbDeleteString")]
        public static extern void DeleteString(IntPtr str);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetStringLen")]
        public static extern IntPtr GetStringLen(IntPtr str);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetString")]
        public static extern IntPtr GetString(IntPtr str);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetProxy")]
        public static extern void SetProxy(IntPtr webView, ref mbProxy proxy);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbSetDebugConfig")]
        public static extern void SetDebugConfig(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string debugString, [MarshalAs(UnmanagedType.LPUTF8Str)] string param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetSetData")]
        public static extern void NetSetData(IntPtr jobPtr, IntPtr buf, int len);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetHookRequest")]
        public static extern void NetHookRequest(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbNetChangeRequestUrl")]
        public static extern void NetChangeRequestUrl(IntPtr jobPtr, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetContinueJob")]
        public static extern void NetContinueJob(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetGetRawHttpHeadInBlinkThread")]
        public static extern IntPtr NetGetRawHttpHeadInBlinkThread(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetGetRawResponseHeadInBlinkThread")]
        public static extern IntPtr NetGetRawResponseHeadInBlinkThread(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetHoldJobToAsynCommit")]
        public static extern void NetHoldJobToAsynCommit(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetCancelRequest")]
        public static extern void NetCancelRequest(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetOnResponse")]
        public static extern void NetOnResponse(IntPtr webviewHandle, mbNetResponseCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetSetWebsocketCallback")]
        public static extern void NetSetWebsocketCallback(IntPtr webview, ref mbWebsocketHookCallbacks callbacks, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetSendWsText")]
        public static extern void NetSendWsText(IntPtr channel, [MarshalAs(UnmanagedType.LPUTF8Str)] string buf, IntPtr len);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetSendWsBlob")]
        public static extern void NetSendWsBlob(IntPtr channel, IntPtr buf, IntPtr len);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbNetEnableResPacket")]
        public static extern void NetEnableResPacket(IntPtr webviewHandle, [MarshalAs(UnmanagedType.LPWStr)] string pathName);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetGetPostBody")]
        public static extern IntPtr NetGetPostBody(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetCreatePostBodyElements")]
        public static extern IntPtr NetCreatePostBodyElements(IntPtr webView, IntPtr length);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetFreePostBodyElements")]
        public static extern void NetFreePostBodyElements(IntPtr elements);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetCreatePostBodyElement")]
        public static extern IntPtr NetCreatePostBodyElement(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetFreePostBodyElement")]
        public static extern void NetFreePostBodyElement(IntPtr element);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbNetCreateWebUrlRequest")]
        public static extern IntPtr NetCreateWebUrlRequest([MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.LPUTF8Str)] string method, [MarshalAs(UnmanagedType.LPUTF8Str)] string mime);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbNetAddHTTPHeaderFieldToUrlRequest")]
        public static extern void NetAddHTTPHeaderFieldToUrlRequest(IntPtr request, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetStartUrlRequest")]
        public static extern int NetStartUrlRequest(IntPtr webView, IntPtr request, IntPtr param, IntPtr callbacks);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetGetHttpStatusCode")]
        public static extern int NetGetHttpStatusCode(IntPtr response);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetGetRequestMethod")]
        public static extern mbRequestType NetGetRequestMethod(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetGetExpectedContentLength")]
        public static extern long NetGetExpectedContentLength(IntPtr response);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetGetResponseUrl")]
        public static extern IntPtr NetGetResponseUrl(IntPtr response); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetCancelWebUrlRequest")]
        public static extern void NetCancelWebUrlRequest(int requestId);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetViewProxy")]
        public static extern void SetViewProxy(IntPtr webView, ref mbProxy proxy);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbNetSetMIMEType")]
        public static extern void NetSetMIMEType(IntPtr jobPtr, [MarshalAs(UnmanagedType.LPUTF8Str)] string type);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNetGetMIMEType")]
        public static extern IntPtr NetGetMIMEType(IntPtr jobPtr);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbNetGetHTTPHeaderField")]
        public static extern IntPtr NetGetHTTPHeaderField(IntPtr job, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, [MarshalAs(UnmanagedType.I1)] bool fromRequestOrResponse);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbNetSetHTTPHeaderField")]
        public static extern void NetSetHTTPHeaderField(IntPtr jobPtr, [MarshalAs(UnmanagedType.LPWStr)] string key, [MarshalAs(UnmanagedType.LPWStr)] string value, [MarshalAs(UnmanagedType.I1)] bool response);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbNetSetHTTPHeaderFieldUtf8")]
        public static extern void NetSetHTTPHeaderFieldUtf8(IntPtr jobPtr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, [MarshalAs(UnmanagedType.I1)] bool response);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetMouseEnabled")]
        public static extern void SetMouseEnabled(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetTouchEnabled")]
        public static extern void SetTouchEnabled(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetSystemTouchEnabled")]
        public static extern void SetSystemTouchEnabled(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetContextMenuEnabled")]
        public static extern void SetContextMenuEnabled(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetNavigationToNewWindowEnable")]
        public static extern void SetNavigationToNewWindowEnable(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetHeadlessEnabled")]
        public static extern void SetHeadlessEnabled(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetDragDropEnable")]
        public static extern void SetDragDropEnable(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetDragEnable")]
        public static extern void SetDragEnable(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetContextMenuItemShow")]
        public static extern void SetContextMenuItemShow(IntPtr webView, mbMenuItemId item, [MarshalAs(UnmanagedType.I1)] bool isShow);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetHandle")]
        public static extern void SetHandle(IntPtr webView, IntPtr wnd);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetHandleOffset")]
        public static extern void SetHandleOffset(IntPtr webView, int x, int y);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetPlatformWindowHandle")]
        public static extern IntPtr GetPlatformWindowHandle(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetHostHWND")]
        public static extern IntPtr GetHostHWND(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetTransparent")]
        public static extern void SetTransparent(IntPtr webviewHandle, [MarshalAs(UnmanagedType.I1)] bool transparent);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetViewSettings")]
        public static extern void SetViewSettings(IntPtr webviewHandle, ref mbViewSettings settings);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetCspCheckEnable")]
        public static extern void SetCspCheckEnable(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetNpapiPluginsEnabled")]
        public static extern void SetNpapiPluginsEnabled(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetMemoryCacheEnable")]
        public static extern void SetMemoryCacheEnable(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbSetCookie")]
        public static extern void SetCookie(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.LPUTF8Str)] string cookie);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetCookieEnabled")]
        public static extern void SetCookieEnabled(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool enable);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbSetCookieJarPath")]
        public static extern void SetCookieJarPath(IntPtr webView, [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbSetCookieJarFullPath")]
        public static extern void SetCookieJarFullPath(IntPtr webView, [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbSetLocalStorageFullPath")]
        public static extern void SetLocalStorageFullPath(IntPtr webView, [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetTitle")]
        public static extern IntPtr GetTitle(IntPtr webView); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbSetWindowTitle")]
        public static extern void SetWindowTitle(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbSetWindowTitleW")]
        public static extern void SetWindowTitleW(IntPtr webView, [MarshalAs(UnmanagedType.LPWStr)] string title);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetUrl")]
        public static extern IntPtr GetUrl(IntPtr webView); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetCursorInfoType")]
        public static extern int GetCursorInfoType(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbAddPluginDirectory")]
        public static extern void AddPluginDirectory(IntPtr webView, [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbSetUserAgent")]
        public static extern void SetUserAgent(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string userAgent);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetZoomFactor")]
        public static extern void SetZoomFactor(IntPtr webView, float factor);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetZoomFactor")]
        public static extern float GetZoomFactor(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetDiskCacheEnabled")]
        public static extern void SetDiskCacheEnabled(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool enable);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbSetDiskCachePath")]
        public static extern void SetDiskCachePath(IntPtr webView, [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetDiskCacheLimit")]
        public static extern void SetDiskCacheLimit(IntPtr webView, IntPtr limit);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetDiskCacheLimitDisk")]
        public static extern void SetDiskCacheLimitDisk(IntPtr webView, IntPtr limit);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetDiskCacheLevel")]
        public static extern void SetDiskCacheLevel(IntPtr webView, int Level);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetResourceGc")]
        public static extern void SetResourceGc(IntPtr webView, int intervalSec);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbIsLoading")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsLoading(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCanGoBackOrForward")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CanGoBackOrForward(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool isGoBack);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCanGoBack")]
        public static extern void CanGoBack(IntPtr webView, mbCanGoBackForwardCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCanGoForward")]
        public static extern void CanGoForward(IntPtr webView, mbCanGoBackForwardCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetCookie")]
        public static extern void GetCookie(IntPtr webView, mbGetCookieCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetCookieOnBlinkThread")]
        public static extern IntPtr GetCookieOnBlinkThread(IntPtr webView); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbClearCookie")]
        public static extern void ClearCookie(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbResize")]
        public static extern void Resize(IntPtr webView, int w, int h);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetSize")]
        public static extern void GetSize(IntPtr webView, out mbRect rc);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetWindowRect")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool GetWindowRect(IntPtr webview, out mbRect rc);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnNavigation")]
        public static extern void OnNavigation(IntPtr webView, mbNavigationCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnNavigationSync")]
        public static extern void OnNavigationSync(IntPtr webView, mbNavigationCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnCreateView")]
        public static extern void OnCreateView(IntPtr webView, mbCreateViewCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnDocumentReady")]
        public static extern void OnDocumentReady(IntPtr webView, mbDocumentReadyCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnPaintUpdated")]
        public static extern void OnPaintUpdated(IntPtr webView, mbPaintUpdatedCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnPaintBitUpdated")]
        public static extern void OnPaintBitUpdated(IntPtr webView, mbPaintBitUpdatedCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnAcceleratedPaint")]
        public static extern void OnAcceleratedPaint(IntPtr webView, mbAcceleratedPaintCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnLoadUrlBegin")]
        public static extern void OnLoadUrlBegin(IntPtr webView, mbLoadUrlBeginCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnLoadUrlEnd")]
        public static extern void OnLoadUrlEnd(IntPtr webView, mbLoadUrlEndCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnLoadUrlFail")]
        public static extern void OnLoadUrlFail(IntPtr webView, mbLoadUrlFailCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnTitleChanged")]
        public static extern void OnTitleChanged(IntPtr webView, mbTitleChangedCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnURLChanged")]
        public static extern void OnURLChanged(IntPtr webView, mbURLChangedCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnLoadingFinish")]
        public static extern void OnLoadingFinish(IntPtr webView, mbLoadingFinishCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnDownload")]
        public static extern void OnDownload(IntPtr webView, mbDownloadCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnDownloadInBlinkThread")]
        public static extern void OnDownloadInBlinkThread(IntPtr webView, mbDownloadInBlinkThreadCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnAlertBox")]
        public static extern void OnAlertBox(IntPtr webView, mbAlertBoxCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnConfirmBox")]
        public static extern void OnConfirmBox(IntPtr webView, mbConfirmBoxCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnPromptBox")]
        public static extern void OnPromptBox(IntPtr webView, mbPromptBoxCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnNetGetFavicon")]
        public static extern void OnNetGetFavicon(IntPtr webView, mbNetGetFaviconCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnConsole")]
        public static extern void OnConsole(IntPtr webView, mbConsoleCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnClose")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool OnClose(IntPtr webView, mbCloseCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnDestroy")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool OnDestroy(IntPtr webView, mbDestroyCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnPrinting")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool OnPrinting(IntPtr webView, mbPrintingCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnDidCreateScriptContext")]
        public static extern void OnDidCreateScriptContext(IntPtr webView, mbDidCreateScriptContextCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnWillReleaseScriptContext")]
        public static extern void OnWillReleaseScriptContext(IntPtr webView, mbWillReleaseScriptContextCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnPluginList")]
        public static extern void OnPluginList(IntPtr webView, mbGetPluginListCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnImageBufferToDataURL")]
        public static extern void OnImageBufferToDataURL(IntPtr webView, mbImageBufferToDataURLCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGoBack")]
        public static extern void GoBack(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGoForward")]
        public static extern void GoForward(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbNavigateAtIndex")]
        public static extern void NavigateAtIndex(IntPtr webView, int index);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetNavigateIndex")]
        public static extern int GetNavigateIndex(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbStopLoading")]
        public static extern void StopLoading(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbReload")]
        public static extern void Reload(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbPerformCookieCommand")]
        public static extern void PerformCookieCommand(IntPtr webView, mbCookieCommand command);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbEditorSelectAll")]
        public static extern void EditorSelectAll(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbEditorCopy")]
        public static extern void EditorCopy(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbEditorCut")]
        public static extern void EditorCut(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbEditorPaste")]
        public static extern void EditorPaste(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbEditorDelete")]
        public static extern void EditorDelete(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbEditorUndo")]
        public static extern void EditorUndo(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbFireMouseEvent")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool FireMouseEvent(IntPtr webView, uint message, int x, int y, uint flags);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbFireContextMenuEvent")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool FireContextMenuEvent(IntPtr webView, int x, int y, uint flags);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbFireMouseWheelEvent")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool FireMouseWheelEvent(IntPtr webView, int x, int y, int delta, uint flags);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbFireKeyUpEvent")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool FireKeyUpEvent(IntPtr webView, uint virtualKeyCode, uint flags, [MarshalAs(UnmanagedType.I1)] bool systemKey);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbFireKeyDownEvent")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool FireKeyDownEvent(IntPtr webView, uint virtualKeyCode, uint flags, [MarshalAs(UnmanagedType.I1)] bool systemKey);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbFireKeyPressEvent")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool FireKeyPressEvent(IntPtr webView, uint charCode, uint flags, [MarshalAs(UnmanagedType.I1)] bool systemKey);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbFireWindowsMessage")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool FireWindowsMessage(IntPtr webView, IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam, out IntPtr result);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetFocus")]
        public static extern void SetFocus(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbKillFocus")]
        public static extern void KillFocus(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbShowWindow")]
        public static extern void ShowWindow(IntPtr webview, int show);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbLoadURL")]
        public static extern void LoadURL(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbLoadHtmlWithBaseUrl")]
        public static extern void LoadHtmlWithBaseUrl(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string html, [MarshalAs(UnmanagedType.LPUTF8Str)] string baseUrl);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbPostURL")]
        public static extern void PostURL(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] postData, int postLen);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetLockedViewDC")]
        public static extern IntPtr GetLockedViewDC(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUnlockViewDC")]
        public static extern void UnlockViewDC(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbWake")]
        public static extern void Wake(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbJsToDouble")]
        public static extern double JsToDouble(IntPtr es, long v);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbJsToBoolean")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool JsToBoolean(IntPtr es, long v);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbJsToString")]
        public static extern IntPtr JsToString(IntPtr es, long v); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbJsToWebFrameHandle")]
        public static extern IntPtr JsToWebFrameHandle(IntPtr es, long v);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetParentWebFrameHandle")]
        public static extern IntPtr GetParentWebFrameHandle(IntPtr webView, IntPtr frame);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetJsValueType")]
        public static extern mbJsType GetJsValueType(IntPtr es, long v);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbJsValueAddRef")]
        public static extern void JsValueAddRef(IntPtr es, long v);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbJsValueDeref")]
        public static extern void JsValueDeref(IntPtr es, long v);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnJsQuery")]
        public static extern void OnJsQuery(IntPtr webView, mbJsQueryCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnJsQueryEx")]
        public static extern void OnJsQueryEx(IntPtr webView, mbJsQueryExCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbResponseQuery")]
        public static extern void ResponseQuery(IntPtr webView, long queryId, int customMsg, [MarshalAs(UnmanagedType.LPUTF8Str)] string response);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbRunJs")]
        public static extern void RunJs(IntPtr webView, IntPtr frameId, [MarshalAs(UnmanagedType.LPUTF8Str)] string script, [MarshalAs(UnmanagedType.I1)] bool isInClosure, mbRunJsCallback callback, IntPtr param, IntPtr unuse);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbRunJsSync")]
        public static extern long RunJsSync(IntPtr webView, IntPtr frameId, [MarshalAs(UnmanagedType.LPUTF8Str)] string script, [MarshalAs(UnmanagedType.I1)] bool isInClosure);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbWebFrameGetMainFrame")]
        public static extern IntPtr WebFrameGetMainFrame(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbIsMainFrame")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsMainFrame(IntPtr webView, IntPtr frameId);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbSetNodeJsEnable")]
        public static extern void SetNodeJsEnable(IntPtr webView, [MarshalAs(UnmanagedType.I1)] bool b);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbSetDeviceParameter")]
        public static extern void SetDeviceParameter(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string device, [MarshalAs(UnmanagedType.LPUTF8Str)] string paramStr, int paramInt, float paramFloat);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetContentAsMarkup")]
        public static extern void GetContentAsMarkup(IntPtr webView, mbGetContentAsMarkupCallback calback, IntPtr param, IntPtr frameId);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetSource")]
        public static extern void GetSource(IntPtr webView, mbGetSourceCallback calback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetWindowScreenshotSync")]
        public static extern IntPtr GetWindowScreenshotSync(IntPtr webView, mbImageFormat format);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetSourceSync")]
        public static extern IntPtr GetSourceSync(IntPtr webView);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUtilSerializeToMHTML")]
        public static extern void UtilSerializeToMHTML(IntPtr webView, mbGetSourceCallback calback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbUtilCreateRequestCode")]
        public static extern IntPtr UtilCreateRequestCode([MarshalAs(UnmanagedType.LPUTF8Str)] string registerInfo); // returns const char*

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbUtilIsRegistered")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool UtilIsRegistered([MarshalAs(UnmanagedType.LPWStr)] string defaultPath);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUtilPrint")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool UtilPrint(IntPtr webView, IntPtr frameId, ref mbPrintSettings printParams);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbUtilBase64Encode")]
        public static extern IntPtr UtilBase64Encode([MarshalAs(UnmanagedType.LPUTF8Str)] string str); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUtilBase64EncodeBuffer")]
        public static extern IntPtr UtilBase64EncodeBuffer(IntPtr str, int len);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbUtilBase64Decode")]
        public static extern IntPtr UtilBase64Decode([MarshalAs(UnmanagedType.LPUTF8Str)] string str); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbUtilDecodeURLEscape")]
        public static extern IntPtr UtilDecodeURLEscape([MarshalAs(UnmanagedType.LPUTF8Str)] string url); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbUtilEncodeURLEscape")]
        public static extern IntPtr UtilEncodeURLEscape([MarshalAs(UnmanagedType.LPUTF8Str)] string url); // returns const utf8*

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbUtilCreateV8Snapshot")]
        public static extern IntPtr UtilCreateV8Snapshot([MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUtilPrintToPdf")]
        public static extern void UtilPrintToPdf(IntPtr webView, IntPtr frameId, ref mbPrintSettings settings, mbPrintPdfDataCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUtilPrintToBitmap")]
        public static extern void UtilPrintToBitmap(IntPtr webView, IntPtr frameId, ref mbScreenshotSettings settings, mbPrintBitmapCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUtilScreenshot")]
        public static extern void UtilScreenshot(IntPtr webView, ref mbScreenshotSettings settings, mbOnScreenshot callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbUtilsSilentPrint")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool UtilsSilentPrint(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string settings);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbPopupDownloadMgr")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool PopupDownloadMgr(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, IntPtr downloadJob);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbPopupDialogAndDownload")]
        public static extern mbDownloadOpt PopupDialogAndDownload(IntPtr webView, ref mbDialogOptions dialogOpt, IntPtr contentLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.LPUTF8Str)] string mime, [MarshalAs(UnmanagedType.LPUTF8Str)] string disposition, IntPtr job, ref mbNetJobDataBind dataBind, ref mbDownloadBind callbackBind);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode, EntryPoint = "mbDownloadByPath")]
        public static extern mbDownloadOpt DownloadByPath(IntPtr webView, ref mbDownloadOptions downloadOptions, [MarshalAs(UnmanagedType.LPWStr)] string path, IntPtr expectedContentLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.LPUTF8Str)] string mime, [MarshalAs(UnmanagedType.LPUTF8Str)] string disposition, IntPtr job, ref mbNetJobDataBind dataBind, ref mbDownloadBind callbackBind);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbDownloadByUtf8Path")]
        public static extern mbDownloadOpt DownloadByUtf8Path(IntPtr webView, ref mbDownloadOptions downloadOptions, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, IntPtr expectedContentLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.LPUTF8Str)] string mime, [MarshalAs(UnmanagedType.LPUTF8Str)] string disposition, IntPtr job, ref mbNetJobDataBind dataBind, ref mbDownloadBind callbackBind);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetPdfPageData")]
        public static extern void GetPdfPageData(IntPtr webView, mbOnGetPdfPageDataCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbCreateMemBuf")]
        public static extern IntPtr CreateMemBuf(IntPtr webView, IntPtr buf, IntPtr length);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbFreeMemBuf")]
        public static extern void FreeMemBuf(IntPtr buf);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbPluginListBuilderAddPlugin")]
        public static extern void PluginListBuilderAddPlugin(IntPtr builder, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string description, [MarshalAs(UnmanagedType.LPUTF8Str)] string fileName);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbPluginListBuilderAddMediaTypeToLastPlugin")]
        public static extern void PluginListBuilderAddMediaTypeToLastPlugin(IntPtr builder, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string description);

        [DllImport(DllName, CallingConvention = CallConv, CharSet = CharSet.Ansi, EntryPoint = "mbPluginListBuilderAddFileExtensionToLastMediaType")]
        public static extern void PluginListBuilderAddFileExtensionToLastMediaType(IntPtr builder, [MarshalAs(UnmanagedType.LPUTF8Str)] string fileExtension);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbEnableHighDPISupport")]
        public static extern void EnableHighDPISupport();

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbRunMessageLoop")]
        public static extern void RunMessageLoop();

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbExitMessageLoop")]
        public static extern void ExitMessageLoop();

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnLoadUrlFinish")]
        public static extern void OnLoadUrlFinish(IntPtr webView, mbLoadUrlFinishCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnLoadUrlHeadersReceived")]
        public static extern void OnLoadUrlHeadersReceived(IntPtr webView, mbLoadUrlHeadersReceivedCallback callback, IntPtr callbackParam);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbOnDocumentReadyInBlinkThread")]
        public static extern void OnDocumentReadyInBlinkThread(IntPtr webView, mbDocumentReadyCallback callback, IntPtr param);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbUtilSetDefaultPrinterSettings")]
        public static extern void UtilSetDefaultPrinterSettings(IntPtr webView, ref mbDefaultPrinterSettings setting);

        [DllImport(DllName, CallingConvention = CallConv, EntryPoint = "mbGetContentWidth")]
        public static extern int GetContentWidth(IntPtr webView);
        #endregion
    }
}
