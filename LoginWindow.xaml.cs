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
            { ShowError("Please enter your email address."); return; }

            if (string.IsNullOrWhiteSpace(password))
            { ShowError("Please enter your password."); return; }

            // Xác thực với database
            var user = DatabaseManager.Login(email, password);
            if (user == null)
            { ShowError("Incorrect email or password."); return; }

            // Đăng nhập thành công
            var main = new MainWindow();
            main.Show();
            Close();
        }

        private void MsLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "Để đăng nhập thật bằng Microsoft, bạn cần tạo Azure AD App Registration và dùng thư viện MSAL.NET.\n\n" +
                "Tính năng này sẽ được kích hoạt khi có Backend Server.", 
                "Microsoft Login Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GuestLogin_Click(object sender, RoutedEventArgs e)
        {
            // Vào thẳng app với tư cách Guest
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
