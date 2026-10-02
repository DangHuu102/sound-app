using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            EmailBox.Text = "Email address";
            EmailBox.Foreground = System.Windows.Media.Brushes.Gray;
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        }

        private void ClearPlaceholder(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            if (tb.Text == "Email address")
            {
                tb.Text = "";
                tb.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void RestorePlaceholder(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = "Email address";
                tb.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void TogglePwd_Click(object sender, RoutedEventArgs e) { }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || email == "Email address")
            {
                ShowError("Please enter your email address.");
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter your password.");
                return;
            }

            // TODO: Gọi API xác thực khi có Backend
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void SignUpLink_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Sign up feature coming soon!", "Sound Studio",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
