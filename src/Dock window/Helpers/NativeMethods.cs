using System.Runtime.InteropServices;
using System.Windows;

namespace DockWindow.Helpers
{
    public static class NativeMethods
    {
        #region Constants

        #region Public
        public const int GWL_EXSTYLE = -20;

        public const int WS_EX_TOOLWINDOW = 0x00000080;

        public const int SC_MOVE = 0xF010;

        public const int
            SWP_NOMOVE = 0x0002,
            SWP_NOSIZE = 0x0001;

        public const int
            WM_ACTIVATE = 0x0006,
            WM_WINDOWPOSCHANGED = 0x0047,
            WM_SYSCOMMAND = 0x0112,
            WM_WINDOWPOSCHANGING = 0x0046;

        public const int
            WM_MOUSEMOVE = 0x200,
            WM_LBUTTONDOWN = 0x201,
            WM_RBUTTONDOWN = 0x204,
            WM_MBUTTONDOWN = 0x207,
            WM_LBUTTONUP = 0x202,
            WM_RBUTTONUP = 0x205,
            WM_MBUTTONUP = 0x208,
            WM_LBUTTONDBLCLK = 0x203,
            WM_RBUTTONDBLCLK = 0x206,
            WM_MBUTTONDBLCLK = 0x209,
            WH_MOUSE_LL = 14;
        #endregion Public

        #endregion Constants

        #region Enums

        #region Public
        public enum ABN
        {
            STATECHANGE,
            POSCHANGED,
            FULLSCREENAPP,
            WINDOWARRANGE
        }

        public enum ABM
        {
            NEW,
            REMOVE,
            QUERYPOS,
            SETPOS,
            GETSTATE,
            GETTASKBARPOS,
            ACTIVATE,
            GETAUTOHIDEBAR,
            SETAUTOHIDEBAR,
            WINDOWPOSCHANGED,
            SETSTATE
        }

        [Flags]
        public enum MONITORINFOF 
        { 
            PRIMARY = 0x1 
        }

        public enum AccentState
        {
            ACCENT_DISABLED,
            ACCENT_ENABLE_GRADIENT,
            ACCENT_ENABLE_TRANSPARENTGRADIENT,
            ACCENT_ENABLE_BLURBEHIND,
            ACCENT_INVALID_STATE,
        }
        #endregion Public

        #endregion Enums

        #region Structures

        #region Public
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT(int x, int y)
        {
            public int x = x;
            public int y = y;

            public static explicit operator Point(POINT p) => new(p.x, p.y);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT(int left, int top, int right, int bottom)
        {
            public int left = left;
            public int top = top;
            public int right = right;
            public int bottom = bottom;

            public readonly int Width { get => right - left; }

            public readonly int Height { get => bottom - top; }

            public static explicit operator Rect(RECT r) => new(r.left, r.top, r.Width, r.Height);

            public static explicit operator RECT(Rect r) => new((int)r.Left, (int)r.Top, (int)r.Right, (int)r.Bottom);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEPOS
        {
            public POINT pt;
            public int hWnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public MONITORINFOF dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        #endregion Public

        #endregion Structures

        #region Delegates

        #region Public
        public delegate bool EnumMonitorsHandler(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        public delegate int MouseHookHandler(int nCode, int wParam, IntPtr lParam);
        #endregion Public

        #endregion Delegates

        #region Methods

        #region Private
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int index, IntPtr newLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr newLong);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        #endregion Private

        #region Public
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int index) => IntPtr.Size == 4 ? GetWindowLongPtr32(hWnd, index) : GetWindowLongPtr64(hWnd, index);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr newLong) => IntPtr.Size == 4 ? SetWindowLongPtr32(hWnd, index, newLong) : SetWindowLongPtr64(hWnd, index, newLong);

        public static int SetWindowComposition(IntPtr hWnd, AccentState state) 
        {
            AccentPolicy accent = new() { AccentState = state };
            int accentStructSize = Marshal.SizeOf(accent);

            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            WindowCompositionAttributeData data = new()
            {
                Attribute = 19,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            int code = SetWindowCompositionAttribute(hWnd, ref data);
            Marshal.FreeHGlobal(accentPtr);

            return code;
        }

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegisterWindowMessage(string msg);

        [DllImport("shell32.dll", ExactSpelling = true)]
        public static extern uint SHAppBarMessage(ABM dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsHandler lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, MouseHookHandler lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
        #endregion Public

        #endregion Methods
    }
}