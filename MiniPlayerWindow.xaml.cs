using System;
using System.Windows;
using System.Windows.Input;
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
                    ThumbnailImage.Source = new BitmapImage(new Uri(thumbnailUrl));
                }
                catch { /* Ignore invalid URI */ }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
