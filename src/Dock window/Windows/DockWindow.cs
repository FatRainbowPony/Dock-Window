using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DockWindow.Addons;
using DockWindow.Helpers;
using static DockWindow.Helpers.NativeMethods;

namespace DockWindow.Windows
{
    public enum DockMode
    {
        Left,
        Top,
        Right,
        Bottom
    }

    public class DockWindow : Window
    {
        #region Fields

        #region Private
        private bool isDockRegistered;
        private bool isDockResized;
        private static readonly int dockMessageId;      
        private GlobalMouseHook? mouseHook = null;
        #endregion Private

        #endregion Fields

        #region Properties

        #region Private
        private static int DockMessageId { get => dockMessageId == 0 ? RegisterWindowMessage("AppBarMessage_EEDFB5206FC3") : dockMessageId; }
        #endregion Private

        #region Public
        public static readonly DependencyProperty DockModeProperty = DependencyProperty.Register(nameof(DockMode), typeof(DockMode), typeof(DockWindow), new FrameworkPropertyMetadata(DockMode.Left, DockLocationChanged));

        public DockMode DockMode
        {
            get => (DockMode)GetValue(DockModeProperty);
            set => SetValue(DockModeProperty, value);
        }

        public static readonly DependencyProperty DockWidthOrHeightProperty = DependencyProperty.Register(nameof(DockWidthOrHeight), typeof(int), typeof(DockWindow), new FrameworkPropertyMetadata(100, DockLocationChanged, DockWidthOrHeightChanged));

        public int DockWidthOrHeight
        {
            get => (int)GetValue(DockWidthOrHeightProperty);
            set => SetValue(DockWidthOrHeightProperty, value);
        }

        public static readonly DependencyProperty AutohideProperty = DependencyProperty.Register(nameof(Autohide), typeof(bool), typeof(DockWindow), new FrameworkPropertyMetadata(false, AutohideChanged));

        public bool Autohide
        {
            get => (bool)GetValue(AutohideProperty);
            set => SetValue(AutohideProperty, value);
        }

        public static readonly DependencyProperty LockAutohideProperty = DependencyProperty.Register(nameof(LockAutohide), typeof(bool), typeof(DockWindow), new PropertyMetadata(false));

        public bool LockAutohide
        {
            get => (bool)GetValue(LockAutohideProperty);
            set => SetValue(LockAutohideProperty, value);
        }

        public static readonly DependencyProperty AnimationBackgroundProperty = DependencyProperty.Register(nameof(AnimationBackground), typeof(Brush), typeof(DockWindow), new FrameworkPropertyMetadata(Brushes.Transparent));

        public Brush AnimationBackground
        {
            get => (Brush)GetValue(AnimationBackgroundProperty);
            set => SetValue(AnimationBackgroundProperty, value);
        }

        public static readonly DependencyProperty MonitorProperty = DependencyProperty.Register(nameof(Monitor), typeof(MonitorInfo), typeof(DockWindow), new FrameworkPropertyMetadata(null, DockLocationChanged));

        public MonitorInfo Monitor
        {
            get => (MonitorInfo)GetValue(MonitorProperty);
            set => SetValue(MonitorProperty, value);
        }
        #endregion Public

        #endregion Properties

        #region Constructors

        #region Public
        static DockWindow()
        {
            WindowStyleProperty.OverrideMetadata(typeof(DockWindow), new FrameworkPropertyMetadata(WindowStyle.None));
            ResizeModeProperty.OverrideMetadata(typeof(DockWindow), new FrameworkPropertyMetadata(ResizeMode.NoResize));
            TopmostProperty.OverrideMetadata(typeof(DockWindow), new FrameworkPropertyMetadata(false));
            ShowInTaskbarProperty.OverrideMetadata(typeof(DockWindow), new FrameworkPropertyMetadata(false));
            MinHeightProperty.OverrideMetadata(typeof(DockWindow), new FrameworkPropertyMetadata(50d, MinMaxWidthOrHeightChanged));
            MinWidthProperty.OverrideMetadata(typeof(DockWindow), new FrameworkPropertyMetadata(50d, MinMaxWidthOrHeightChanged));
            MaxHeightProperty.OverrideMetadata(typeof(DockWindow), new FrameworkPropertyMetadata(MinMaxWidthOrHeightChanged));
            MaxWidthProperty.OverrideMetadata(typeof(DockWindow), new FrameworkPropertyMetadata(MinMaxWidthOrHeightChanged));
        }
        #endregion Public

        #endregion Constructors

        #region Methods

        #region Private
        private static void DockLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DockWindow dockWindow && dockWindow.isDockRegistered)
            {
                dockWindow.DockLocationChanged(false);
            }
        }

        private void DockLocationChanged(bool ignoreAnimation = true)
        {
            if (!isDockResized)
            {
                APPBARDATA abd = GetAppBarData();
                abd.rc = (RECT)GetCurrentMonitor().ViewportBounds;
                _ = SHAppBarMessage(ABM.QUERYPOS, ref abd);

                int dockWidthOrHeightInDesktopPixels = WpfDimensionToDesktop(DockWidthOrHeight);

                switch (DockMode)
                {
                    case DockMode.Top:
                        abd.rc.bottom = abd.rc.top + dockWidthOrHeightInDesktopPixels;
                        break;

                    case DockMode.Bottom:
                        abd.rc.top = abd.rc.bottom - dockWidthOrHeightInDesktopPixels;
                        break;

                    case DockMode.Left:
                        abd.rc.right = abd.rc.left + dockWidthOrHeightInDesktopPixels;
                        break;

                    case DockMode.Right:
                        abd.rc.left = abd.rc.right - dockWidthOrHeightInDesktopPixels;
                        break;
                }

                _ = SHAppBarMessage(ABM.SETPOS, ref abd);

                isDockResized = true;

                Rect rect = (Rect)abd.rc;

                try
                {
                    Left = DesktopDimensionToWpf(rect.Left);
                    Top = DesktopDimensionToWpf(rect.Top);
                    Width = DesktopDimensionToWpf(rect.Width);
                    Height = DesktopDimensionToWpf(rect.Height);
                }
                finally { isDockResized = false; }

                if (!ignoreAnimation && Autohide)
                {
                    RemoveAutohideAnimation();
                    AddAutohideAnimation();
                }
            }
        }

        private static object DockWidthOrHeightChanged(DependencyObject d, object baseValue)
        {
            if (d is DockWindow dockWindow && baseValue is int newValue)
            {
                if (dockWindow.DockMode == DockMode.Left || dockWindow.DockMode == DockMode.Right)
                {
                    baseValue = DoubleToInt(newValue, dockWindow.MinWidth, dockWindow.MaxWidth);
                }
                else
                {
                    baseValue = DoubleToInt(newValue, dockWindow.MinHeight, dockWindow.MaxHeight);
                }
            }

            return baseValue;
        }

        private static void MinMaxWidthOrHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => d.CoerceValue(DockWidthOrHeightProperty);

        private static void AutohideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DockWindow dockWindow && e.NewValue is bool autohide)
            {
                APPBARDATA abd = dockWindow.GetAppBarData();
                _ = SHAppBarMessage(ABM.REMOVE, ref abd);

                if (autohide)
                {
                    _ = SHAppBarMessage(ABM.SETAUTOHIDEBAR, ref abd);

                    dockWindow.AddMouseHook();
                    dockWindow.AddAutohideAnimation();
                    dockWindow.Visibility = Visibility.Hidden;
                }
                else
                {
                    _ = SHAppBarMessage(ABM.NEW, ref abd);

                    dockWindow.RemoveMouseHook();
                    dockWindow.RemoveAutohideAnimation();
                    dockWindow.Visibility = Visibility.Visible;
                }

                dockWindow.DockLocationChanged();
            }
        }

        private void MouseAction(GlobalMouseEventArgs e)
        {
            if (Monitor != null && e.MouseState == MouseState.Move)
            {
                int mouseX = e.Location.X >= 0 ? (int)e.Location.X : 0,
                    mouseY = e.Location.Y >= 0 ? (int)e.Location.Y : 0;

                int edgeX1 = 0,
                    edgeY1 = 0,
                    edgeX2 = 0,
                    edgeY2 = 0,
                    edgeX3 = 0,
                    edgeY3 = 0,
                    edgeX4 = 0,
                    edgeY4 = 0;

                int dockX1 = (int)Left,
                    dockY1 = (int)Top,
                    dockX2 = dockX1 + (int)Width,
                    dockY2 = dockY1,
                    dockX3 = dockX2,
                    dockY3 = dockY1 + (int)Height,
                    dockX4 = dockX1,
                    dockY4 = dockY3;

                if (Visibility != Visibility.Visible)
                {
                    switch (DockMode)
                    {
                        case DockMode.Left:
                            edgeX1 = (int)Monitor.ViewportBounds.TopLeft.X;
                            edgeY1 = (int)Monitor.ViewportBounds.TopLeft.Y;

                            edgeX2 = (int)Monitor.ViewportBounds.TopLeft.X + 15;
                            edgeY2 = edgeY1;

                            edgeX3 = edgeX2;
                            edgeY3 = (int)Monitor.ViewportBounds.BottomLeft.Y;

                            edgeX4 = edgeX1;
                            edgeY4 = edgeY3;
                            break;

                        case DockMode.Top:
                            edgeX1 = (int)Monitor.ViewportBounds.TopLeft.X;
                            edgeY1 = (int)Monitor.ViewportBounds.TopLeft.Y;

                            edgeX2 = (int)Monitor.ViewportBounds.TopRight.X;
                            edgeY2 = (int)Monitor.ViewportBounds.TopRight.Y;

                            edgeX3 = edgeX2;
                            edgeY3 = (int)Monitor.ViewportBounds.TopRight.Y + 15;

                            edgeX4 = edgeX1;
                            edgeY4 = edgeY3;
                            break;

                        case DockMode.Right:
                            edgeX1 = (int)Monitor.ViewportBounds.TopRight.X - 15;
                            edgeY1 = (int)Monitor.ViewportBounds.TopRight.Y;

                            edgeX2 = (int)Monitor.ViewportBounds.TopRight.X;
                            edgeY2 = edgeY1;

                            edgeX3 = (int)Monitor.ViewportBounds.BottomRight.X;
                            edgeY3 = (int)Monitor.ViewportBounds.BottomRight.Y;

                            edgeX4 = edgeX1;
                            edgeY4 = edgeY3;
                            break;

                        case DockMode.Bottom:
                            edgeX1 = (int)Monitor.ViewportBounds.BottomLeft.X;
                            edgeY1 = (int)Monitor.ViewportBounds.BottomLeft.Y - 15;

                            edgeX2 = (int)Monitor.ViewportBounds.BottomRight.X;
                            edgeY2 = edgeY1;

                            edgeX3 = (int)Monitor.ViewportBounds.BottomRight.X;
                            edgeY3 = (int)Monitor.ViewportBounds.BottomRight.Y;

                            edgeX4 = edgeX1;
                            edgeY4 = (int)Monitor.ViewportBounds.BottomLeft.Y;
                            break;
                    }

                    if (mouseX >= edgeX1 && mouseX >= edgeX4 && mouseX <= edgeX2 && mouseX <= edgeX3 &&
                        mouseY >= edgeY1 && mouseY <= edgeY4 && mouseY >= edgeY2 && mouseY <= edgeY3)
                    {
                        Visibility = Visibility.Visible;
                        Topmost = true;
                    }
                }

                if (Visibility == Visibility.Visible)
                {
                    if (!(mouseX >= dockX1 && mouseY >= dockY1 &&
                        mouseX <= dockX2 && mouseY >= dockY2 &&
                        mouseX <= dockX3 && mouseY <= dockY3 &&
                        mouseX >= dockX4 && mouseY <= dockY4))
                    {
                        Visibility = Visibility.Hidden;
                        Topmost = false;
                    }
                }
            }
        }

        private static int DoubleToInt(int value, double min, double max)
        {
            if (min > value)
            {
                value = (int)Math.Ceiling(min);
            }
            if (max < value)
            {
                value = (int)Math.Floor(max);
            }

            return value;
        }

        private int WpfDimensionToDesktop(double dim) => (int)Math.Ceiling(dim * VisualTreeHelper.GetDpi(this).PixelsPerDip);

        private double DesktopDimensionToWpf(double dim) => dim / VisualTreeHelper.GetDpi(this).PixelsPerDip;

        private MonitorInfo GetCurrentMonitor()
        {
            MonitorInfo? currentMonitor = Monitor;
            List<MonitorInfo> allMonitors = [.. MonitorInfo.GetAllMonitors()];

            if (currentMonitor == null || !allMonitors.Contains(currentMonitor))
            {
                currentMonitor = allMonitors.First(monitor => monitor.IsPrimary);
            }

            return currentMonitor;
        }

        private APPBARDATA GetAppBarData() => new()
        {
            cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
            hWnd = new WindowInteropHelper(this).Handle,
            uCallbackMessage = DockMessageId,
            uEdge = (int)DockMode
        };

        private void AddMouseHook()
        {
            mouseHook = GlobalMouseHook.Create();
            mouseHook.AddMouseActionHandler(MouseAction);
            mouseHook.Start();
        }

        private void RemoveMouseHook()
        {
            mouseHook?.Stop();
            mouseHook?.RemoveMouseAction(MouseAction);
            mouseHook = null;
        }

        private void AddAutohideAnimation()
        {
            if (Content is Grid rootGrid && 
                FindAnimationGrid(rootGrid) is Grid animGrid &&
                FindAnimationRectangle(animGrid) is Rectangle animRect)
            {
                TransformGroup transGroup = new();
                transGroup.Children.Add(new ScaleTransform());
                transGroup.Children.Add(new SkewTransform());
                transGroup.Children.Add(new RotateTransform());
                transGroup.Children.Add(new TranslateTransform());

                animGrid.RenderTransform = transGroup;

                DoubleAnimationUsingKeyFrames posAnim = new() { KeyFrames = [] };
                posAnim.KeyFrames.Add(new EasingDoubleKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                    Value = DockMode == DockMode.Left || DockMode == DockMode.Top ? -DockWidthOrHeight : DockWidthOrHeight
                });
                posAnim.KeyFrames.Add(new EasingDoubleKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1)),
                    Value = 0
                });
                if (DockMode == DockMode.Left || DockMode == DockMode.Right)
                {
                    Storyboard.SetTargetProperty(posAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.X)"));
                }
                else
                {
                    Storyboard.SetTargetProperty(posAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.Y)"));
                }

                Storyboard gridBoard = new() { Children = [], SpeedRatio = 3 };
                gridBoard.Children.Add(posAnim);

                DataTrigger gridTrigger = new()
                {
                    Binding = new Binding { RelativeSource = new RelativeSource { AncestorType = typeof(DockWindow) }, Path = new PropertyPath("Visibility") },
                    Value = Visibility.Visible
                };
                gridTrigger.EnterActions.Add(new BeginStoryboard { Storyboard = gridBoard });

                Style gridStyle = new(typeof(Grid));
                gridStyle.Triggers.Add(gridTrigger);

                animGrid.Style = gridStyle;

                DoubleAnimationUsingKeyFrames opacityAnim = new() { KeyFrames = [] };
                opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)), Value = 0.1 });
                opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1)), Value = 0.5 });
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("(UIElement.Opacity)"));

                Storyboard rectBoard = new() { Children = [], SpeedRatio = 3 };
                rectBoard.Children.Add(opacityAnim);

                DataTrigger rectTrigger = new()
                {
                    Binding = new Binding { RelativeSource = new RelativeSource { AncestorType = typeof(DockWindow) }, Path = new PropertyPath("Visibility") },
                    Value = Visibility.Visible
                };
                rectTrigger.EnterActions.Add(new BeginStoryboard { Storyboard = rectBoard });

                Style rectStyle = new(typeof(Rectangle));
                rectStyle.Triggers.Add(rectTrigger);

                animRect.Style = rectStyle;
            }
        }

        private void RemoveAutohideAnimation()
        {
            if (Content is Grid rootGrid && 
                FindAnimationGrid(rootGrid) is Grid animGrid &&
                FindAnimationRectangle(animGrid) is Rectangle animRect)
            {
                animGrid.Style = null;
                animRect.Style = null;
            }
        }

        private static Rectangle? FindAnimationRectangle(Grid grid)
        {
            foreach (object child in grid.Children)
            {
                if (child is Rectangle rect && rect.Name == "PART_AnimRect")
                {
                    return rect;
                }
            }

            return null;
        }

        private static Grid? FindAnimationGrid(Grid rootGrid)
        {
            foreach (object child in rootGrid.Children)
            {
                if (child is Grid childGrid && childGrid.Name == "PART_AnimGrid")
                {
                    return childGrid;
                }
            }

            return null;
        }
        #endregion Private

        #region Protected
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            object oldContent = Content;

            Content = null;
            Content = new Grid();

            Rectangle backRect = new()
            {
                Name = "PART_AnimRect",
                RadiusX = 10,
                RadiusY = 10,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = 0.5
            };
            backRect.SetBinding(Shape.FillProperty, new Binding { RelativeSource = new RelativeSource { AncestorType = typeof(DockWindow) }, Path = new PropertyPath("AnimationBackground") });
            Panel.SetZIndex(backRect, -1);

            Grid animGrid = new() { Name = "PART_AnimGrid" };
            animGrid.Children.Add((UIElement)oldContent);
            animGrid.Children.Insert(1, backRect);

            Grid rootGrid = (Grid)Content;
            rootGrid.Children.Add(animGrid);

            HwndSource source = (HwndSource)PresentationSource.FromVisual(this);

            if (!ShowInTaskbar)
            {
                ulong exstyle =(ulong)GetWindowLongPtr(source.Handle, GWL_EXSTYLE);
                exstyle |= WS_EX_TOOLWINDOW;
                SetWindowLongPtr(source.Handle, GWL_EXSTYLE, unchecked((IntPtr)exstyle));
            }

            SetWindowComposition(new WindowInteropHelper(this).Handle, AccentState.ACCENT_ENABLE_BLURBEHIND);

            source.AddHook(new HwndSourceHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) => 
            {
                if (msg == WM_WINDOWPOSCHANGING && !isDockResized)
                {
                    WINDOWPOS wPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    wPos.flags |= SWP_NOMOVE | SWP_NOSIZE;
                    Marshal.StructureToPtr(wPos, lParam, false);
                }
                else if (msg == WM_ACTIVATE)
                {
                    APPBARDATA abd = GetAppBarData();
                    _ = SHAppBarMessage(ABM.ACTIVATE, ref abd);
                }
                else if (msg == WM_WINDOWPOSCHANGED)
                {
                    APPBARDATA abd = GetAppBarData();
                    _ = SHAppBarMessage(ABM.WINDOWPOSCHANGED, ref abd);
                }
                else if (msg == DockMessageId && (ABN)checked((int)wParam) == ABN.POSCHANGED)
                {
                    DockLocationChanged();

                    handled = true;
                }

                return IntPtr.Zero;
            }));

            APPBARDATA abd = GetAppBarData();
            _ = SHAppBarMessage(Autohide ? ABM.SETAUTOHIDEBAR : ABM.NEW, ref abd);

            if (Autohide)
            {
                AddMouseHook();
                AddAutohideAnimation();

                Visibility = Visibility.Hidden;
            }

            isDockRegistered = true;

            DockLocationChanged();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (e.Cancel)
            {
                return;
            }

            if (isDockRegistered) 
            {
                if (Autohide)
                {
                    RemoveMouseHook();
                    RemoveAutohideAnimation();
                }

                APPBARDATA abd = GetAppBarData();
                _ = SHAppBarMessage(ABM.REMOVE, ref abd);

                isDockRegistered = false;
            }
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);

            DockLocationChanged();
        }
        #endregion Protected

        #endregion Methods
    }
}