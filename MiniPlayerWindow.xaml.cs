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
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.Show();
            this.Close();
        }
    }
}
