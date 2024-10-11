using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova
{
    public static partial class MiniblinkNative
    {
        private const string LIBRARY_NAME = "miniblink";

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool mbCloseCallback(IntPtr webView, IntPtr param, IntPtr unuse);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void mbThreadCallback(IntPtr param1, IntPtr param2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void mbJsQueryCallback(IntPtr webView, IntPtr param, IntPtr es, long queryId, int customMsg, IntPtr request);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void mbRunJsCallback(IntPtr webView, IntPtr param, IntPtr es, long v);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void mbConsoleCallback(IntPtr webView, IntPtr param, mbConsoleLevel level, IntPtr message, IntPtr sourceName, uint sourceLine, IntPtr stackTrace);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool mbLoadUrlBeginCallback(IntPtr webView, IntPtr param, IntPtr url, IntPtr job);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbInit(IntPtr settings);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr mbCreateWebWindow(mbWindowType type, IntPtr parent, int x, int y, int width, int height);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbShowWindow(IntPtr webWindow, bool showFlag);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbLoadURL(IntPtr webWindow, IntPtr url);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbRunMessageLoop();

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern bool mbOnClose(IntPtr webView, mbCloseCallback callback, IntPtr param);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbCallUiThreadAsync(mbThreadCallback callback, IntPtr param1, IntPtr param2);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbOnJsQuery(IntPtr webView, mbJsQueryCallback callback, IntPtr param);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbRunJs(IntPtr webView, IntPtr frameId, IntPtr script, bool isInClosure, mbRunJsCallback callback, IntPtr param, IntPtr unuse);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbOnConsole(IntPtr webView, mbConsoleCallback callback, IntPtr param);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbOnLoadUrlBegin(IntPtr webView, mbLoadUrlBeginCallback callback, IntPtr param);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbNetSetMIMEType(IntPtr jobPtr, IntPtr type);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void mbNetSetData(IntPtr jobPtr, [MarshalAs(UnmanagedType.LPArray)] byte[] buf, int len);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr mbWebFrameGetMainFrame(IntPtr webView);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbMoveToCenter(IntPtr webview);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbSetCspCheckEnable(IntPtr webview, bool b);

        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void mbSetWindowTitle(IntPtr webview, IntPtr title);

        public enum mbConsoleLevel
        {
            mbLevelLog = 1,
            mbLevelWarning = 2,
            mbLevelError = 3,
            mbLevelDebug = 4,
            mbLevelInfo = 5,
            mbLevelRevokedError = 6,
            mbLevelLast = mbLevelInfo
        }

        public enum mbWindowType
        {
            WKE_WINDOW_TYPE_POPUP,
            WKE_WINDOW_TYPE_TRANSPARENT,
            WKE_WINDOW_TYPE_CONTROL
        }
    }
}
