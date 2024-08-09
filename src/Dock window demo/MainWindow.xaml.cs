using System.Windows;
using DockWindow.Addons;
using DockWindow.Windows;

namespace DockWindowDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DockWindow.Windows.DockWindow
    {
        #region Constructors

        #region Public
        public MainWindow()
        {
            InitializeComponent();

            Monitors.ItemsSource = MonitorInfo.GetAllMonitors();
            Monitors.SelectedIndex = 0;

            Modes.ItemsSource = new[]
            {
                DockMode.Left,
                DockMode.Right,
                DockMode.Top,
                DockMode.Bottom
            };
            Modes.SelectedIndex = 2;
        }
        #endregion Public

        #endregion Constructors

        #region Methods

        #region Private
        private void CloseClick(object sender, RoutedEventArgs e) => Close();
        #endregion Private

        #endregion Methods
    }
}