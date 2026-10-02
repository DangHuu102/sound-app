using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace soundapp
{
    public partial class MiniPlayerWindow : Window
    {
        private MainWindow _parentWindow;

        public MiniPlayerWindow(MainWindow parent, string title, string thumbnailUrl)
        {
            InitializeComponent();
            _parentWindow = parent;

            TitleText.Text = title;

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(thumbnailUrl);
                    bmp.DecodePixelWidth = 280;
                    bmp.CacheOption = BitmapCacheOption.None;
                    bmp.EndInit();
                    ThumbnailImage.Source = bmp;
                }
                catch { }
            }

            // Đợi layout xong mới tính chiều rộng để animate
            Loaded += (s, e) => StartMarquee();
        }

        private void StartMarquee()
        {
            // Đo độ rộng thực của chữ sau khi render
            TitleText.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = TitleText.DesiredSize.Width;
            double containerWidth = 236; // Width của window trừ margin

            // Nếu chữ ngắn hơn container thì không cần chạy
            if (textWidth <= containerWidth)
            {
                MarqueeTransform.X = 0;
                return;
            }

            // Chạy từ vị trí 0 sang bên trái (âm), sau đó reset
            double totalDistance = textWidth + 30;
            double durationSecs = totalDistance / 60.0; // 60px/s

            var animation = new DoubleAnimation
            {
                From = containerWidth,       // Bắt đầu từ bên phải
                To = -textWidth,             // Chạy sang trái hết
                Duration = TimeSpan.FromSeconds(durationSecs),
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = null
            };

            MarqueeTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.Show();
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
