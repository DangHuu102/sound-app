using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DatabaseManager.InitializeDatabase();

            EmailBox.Text = "Enter your email";
            EmailBox.Foreground = System.Windows.Media.Brushes.Gray;
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        }

        private void ClearPlaceholder(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            if (tb.Text == "Enter your email")
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
                tb.Text = "Enter your email";
                tb.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || email == "Enter your email")
            { ShowError("Please enter your email."); return; }

            if (string.IsNullOrWhiteSpace(password))
            { ShowError("Please enter your password."); return; }

            var user = DatabaseManager.Login(email, password);
            if (user == null)
            { ShowError("Incorrect email or password."); return; }

            var main = new MainWindow();
            main.Show();
            Close();
        }

        private void SignUpLink_Click(object sender, RoutedEventArgs e)
        {
            var signUp = new SignUpWindow();
            signUp.Show();
            Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var main = new MainWindow();
            main.Show();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
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
