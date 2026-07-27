using System;
using System.Windows;
using PP2_Live_Tracker.Helpers;
using PP2_Live_Tracker.Services;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace PP2_Live_Tracker
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {

            StatusText.Text = "🟡 Etsitään Pro Pilkki 2...";

            IntPtr gameWindow = WindowFinder.FindProPilkkiWindow();

            if (gameWindow != IntPtr.Zero)
            {
                StatusText.Text = "🟢 Pro Pilkki 2 löytyi!";
            }
            else
            {
                StatusText.Text = "🔴 Pro Pilkki 2 -ikkunaa ei löytynyt.";
            }
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            var rect = WindowFinder.GetGameWindowRect();

            if (rect == null)
            {
                StatusText.Text = "🔴 Ei peliä";
                PreviewImage.Source = null;
                return;
            }

            int width = rect.Value.Right - rect.Value.Left;
            int height = rect.Value.Bottom - rect.Value.Top;

            StatusText.Text = $"🟢 {width} × {height}";

            PreviewImage.Source = ScreenCaptureService.CaptureArea(
                rect.Value.Left,
                rect.Value.Top,
                width,
                height);
        }

    }
}