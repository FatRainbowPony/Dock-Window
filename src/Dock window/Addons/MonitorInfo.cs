using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using static DockWindow.Helpers.NativeMethods;

namespace DockWindow.Addons
{
    public sealed class MonitorInfo : IEquatable<MonitorInfo>
    {
        #region Properties

        #region Public
        public string DeviceId { get; private set; }

        public Rect ViewportBounds { get; private set; }

        public Rect WorkAreaBounds { get; private set; }

        public bool IsPrimary { get; private set; }
        #endregion Public

        #endregion Properties

        #region Constructors

        #region Internal
        internal MonitorInfo(MONITORINFOEX mi)
        {
            DeviceId = mi.szDevice;
            ViewportBounds = (Rect)mi.rcMonitor;
            WorkAreaBounds = (Rect)mi.rcWork;
            IsPrimary = mi.dwFlags.HasFlag(MONITORINFOF.PRIMARY);
        }
        #endregion Internal

        #endregion Constructors

        #region Methods

        #region Public
        public override string ToString() => DeviceId;

        public override int GetHashCode() => DeviceId.GetHashCode();

        public override bool Equals(object? obj) => obj != null && Equals((MonitorInfo)obj);

        public bool Equals(MonitorInfo? other) => DeviceId == other?.DeviceId;

        public static IEnumerable<MonitorInfo> GetAllMonitors()
        {
            List<MonitorInfo> monitors = [];

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, new EnumMonitorsHandler((IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX mi = new() { cbSize = Marshal.SizeOf(typeof(MONITORINFOEX)) };
                if (!GetMonitorInfo(hMonitor, ref mi))
                {
                    throw new Win32Exception();
                }

                monitors.Add(new MonitorInfo(mi));

                return true;
            }), IntPtr.Zero);

            return monitors;
        }
        #endregion Public

        #endregion Methods
    }
}