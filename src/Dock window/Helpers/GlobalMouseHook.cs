using System.Reflection;
using System.Runtime.InteropServices;
using DockWindow.Addons;
using static DockWindow.Helpers.NativeMethods;

namespace DockWindow.Helpers
{
    public sealed class GlobalMouseHook
    {
        #region Fields

        #region Private
        private readonly object operationLock = new();
        private readonly static object creationLock = new();
        private MouseHookHandler? hookHandler;
        private int handleHook;
        private bool isStarted;
        #endregion Private

        #endregion Fields

        #region Delegates

        #region Public
        public delegate void MouseActionHandler(GlobalMouseEventArgs e);
        #endregion Public

        #endregion Delegates

        #region Events

        #region Private
        private event MouseActionHandler? MouseAction;
        #endregion Private

        #endregion Events

        #region Constructors

        #region Private
        private GlobalMouseHook() { }
        #endregion Private

        #endregion Constructors

        #region Destructors
        ~GlobalMouseHook() => Stop();
        #endregion Destructors

        #region Methods

        #region Public
        public static GlobalMouseHook Create()
        {
            lock (creationLock)
            {
                return new GlobalMouseHook();
            }
        }

        public void Start()
        {
            if (!isStarted)
            {
                lock (operationLock) 
                {
                    if (MouseAction == null)
                    {
                        throw new Exception("Mouse action handler is not set");
                    }

                    if (handleHook == 0)
                    {
                        hookHandler = new MouseHookHandler((int nCode, int wParam, IntPtr lParam) => 
                        {
                            if (nCode >= 0 && MouseAction != null)
                            {
                                MouseAction(new GlobalMouseEventArgs(wParam, (MOUSEPOS)Marshal.PtrToStructure(lParam, typeof(MOUSEPOS))!));
                            }

                            return CallNextHookEx(handleHook, nCode, wParam, lParam);
                        });

                        handleHook = SetWindowsHookEx(WH_MOUSE_LL, hookHandler, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
                        if (handleHook == 0)
                        {
                            Stop();

                            throw new Exception("Hook mouse failed");
                        }
                    }

                    isStarted = true;
                }
            }
        }

        public void Stop()
        {
            if (isStarted)
            {
                lock (operationLock) 
                {
                    bool restMouse = true;

                    if (handleHook != 0)
                    {
                        restMouse = UnhookWindowsHookEx(handleHook);
                        handleHook = 0;
                    }

                    if (!restMouse)
                    {
                        throw new Exception("Unhook mouse failed");
                    }

                    MouseAction = null;
                    isStarted = false;
                }
            }
        }

        public void AddMouseActionHandler(MouseActionHandler handler) => MouseAction += handler;

        public void RemoveMouseAction(MouseActionHandler handler)
        {
            if (MouseAction != null)
            {
                MouseAction -= handler;
            }
        }
        #endregion Public

        #endregion Methods
    }
}