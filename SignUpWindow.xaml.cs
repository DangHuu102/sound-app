using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class SignUpWindow : Window
    {
        public SignUpWindow()
        {
            InitializeComponent();
            NameBox.Text = "Display name";
            NameBox.Foreground = System.Windows.Media.Brushes.Gray;
            NameBox.Tag = "Display name";

            EmailBox.Text = "Email address";
            EmailBox.Foreground = System.Windows.Media.Brushes.Gray;
            EmailBox.Tag = "Email address";

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

            if (string.IsNullOrWhiteSpace(name) || name == "Display name")
            { ShowError("Please enter your display name."); return; }

            if (string.IsNullOrWhiteSpace(email) || email == "Email address" || !email.Contains("@"))
            { ShowError("Please enter a valid email address."); return; }

            if (password.Length < 6)
            { ShowError("Password must be at least 6 characters."); return; }

            // MOCK GỬI EMAIL: Tạo mã OTP 6 số ngẫu nhiên
            string otpCode = new System.Random().Next(100000, 999999).ToString();
            var otpDialog = new VerifyOtpWindow(email, otpCode) { Owner = this };

            if (otpDialog.ShowDialog() == true)
            {
                // OTP đúng -> Lưu user
                bool success = DatabaseManager.Register(email, password, name);
                if (!success)
                { ShowError("This email is already registered."); return; }

                var main = new MainWindow();
                main.Show();
                Close();
            }
        }

        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            Close();
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
