using System.Windows;
using static DockWindow.Helpers.NativeMethods;

namespace DockWindow.Addons
{
    public enum MouseState
    {
        None,
        Move,
        LeftButtonDown,
        RightButtonDown,
        MiddleButtonDown,
        LeftButtonUp,
        RightButtonUp,
        MiddleButtonUp,
        LeftButtonClick,
        RightButtonClick,
        MiddleButtonClick
    }

    public sealed class GlobalMouseEventArgs
    {
        #region Properties

        #region Public
        public MouseState MouseState { get; private set; }

        public Point Location { get; private set; }
        #endregion Public

        #endregion Properties

        #region Constructors

        #region Internal
        internal GlobalMouseEventArgs(int wParam, MOUSEPOS mPos)
        {
            MouseState = wParam switch
            {
                WM_MOUSEMOVE => MouseState.Move,
                WM_LBUTTONDOWN => MouseState.LeftButtonDown,
                WM_RBUTTONDOWN => MouseState.RightButtonDown,
                WM_MBUTTONDOWN => MouseState.MiddleButtonDown,
                WM_LBUTTONUP => MouseState.LeftButtonUp,
                WM_RBUTTONUP => MouseState.RightButtonUp,
                WM_MBUTTONUP => MouseState.MiddleButtonUp,
                WM_LBUTTONDBLCLK => MouseState.LeftButtonClick,
                WM_RBUTTONDBLCLK => MouseState.RightButtonClick,
                WM_MBUTTONDBLCLK => MouseState.MiddleButtonClick,
                _ => MouseState.None
            };
            Location = (Point)mPos.pt;
        }
        #endregion Internal

        #endregion Constructors
    }
}