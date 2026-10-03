using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class SignUpWindow : Window
    {
        public SignUpWindow()
        {
            InitializeComponent();
            NameBox.Text = "Enter your username";
            NameBox.Foreground = System.Windows.Media.Brushes.Gray;
            NameBox.Tag = "Enter your username";

            EmailBox.Text = "Enter your email";
            EmailBox.Foreground = System.Windows.Media.Brushes.Gray;
            EmailBox.Tag = "Enter your email";

            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        }

        private void ClearPlaceholder(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            if (tb.Text == tb.Tag?.ToString())
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
                tb.Text = tb.Tag?.ToString() ?? "";
                tb.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SignUpBtn_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirm = ConfirmBox.Password;

            if (string.IsNullOrWhiteSpace(name) || name == "Enter your username")
            { ShowError("Please enter your username."); return; }

            if (string.IsNullOrWhiteSpace(email) || email == "Enter your email" || !email.Contains("@"))
            { ShowError("Please enter a valid email address."); return; }

            if (password.Length < 6)
            { ShowError("Password must be at least 6 characters."); return; }

            if (password != confirm)
            { ShowError("Passwords do not match."); return; }

            string otpCode = new System.Random().Next(100000, 999999).ToString();
            var otpDialog = new VerifyOtpWindow(email, otpCode) { Owner = this };

            if (otpDialog.ShowDialog() == true)
            {
                bool success = DatabaseManager.Register(email, password, name);
                if (!success)
                { ShowError("This email is already registered."); return; }

                GoToMain();
            }
        }

        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Hide();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            GoToMain();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void GoToMain()
        {
            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w is MainWindow mw)
                {
                    mw.Show();
                    this.Hide();
                    return;
                }
            }
            var main = new MainWindow();
            main.Show();
            this.Hide();
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
