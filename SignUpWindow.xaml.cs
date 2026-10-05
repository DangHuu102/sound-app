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

            NameBox.PreviewMouseLeftButtonDown += (s, e) => NameBox.Focus();
            EmailBox.PreviewMouseLeftButtonDown += (s, e) => EmailBox.Focus();
            PasswordBox.PreviewMouseLeftButtonDown += (s, e) => PasswordBox.Focus();
            UniversityBox.PreviewMouseLeftButtonDown += (s, e) => UniversityBox.Focus();
            StudentIdBox.PreviewMouseLeftButtonDown += (s, e) => StudentIdBox.Focus();

            MouseLeftButtonDown += (s, e) => 
            { 
                if (e.OriginalSource is System.Windows.DependencyObject depObj)
                {
                    var current = depObj;
                    while (current != null)
                    {
                        if (current is System.Windows.Controls.TextBox || current is System.Windows.Controls.PasswordBox || current is System.Windows.Controls.Button)
                        {
                            return;
                        }
                        current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                    }
                }

                if (e.ButtonState == MouseButtonState.Pressed)
                    DragMove(); 
            };
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

        private async void SignUpBtn_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string university = UniversityBox.Text.Trim();
            string studentId = StudentIdBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || name == "Enter your username")
            { ShowError("Please enter your username."); return; }

            if (string.IsNullOrWhiteSpace(email) || email == "Enter your email" || !email.Contains("@"))
            { ShowError("Please enter a valid email address."); return; }

            if (password.Length < 6)
            { ShowError("Password must be at least 6 characters."); return; }

            if (string.IsNullOrWhiteSpace(university) || university == "Ex: ICTU")
            { ShowError("Please enter your University."); return; }

            if (string.IsNullOrWhiteSpace(studentId) || studentId == "Ex: DTC255190009")
            { ShowError("Please enter your Student ID."); return; }

            string otpCode = new System.Random().Next(100000, 999999).ToString();

            // Thử gửi email thật
            if (EmailService.IsConfigured)
            {
                // Hiện trạng thái đang gửi
                ErrorText.Foreground = System.Windows.Media.Brushes.CornflowerBlue;
                ErrorText.Text = $"Đang gửi mã xác thực đến {email}...";
                ErrorText.Visibility = System.Windows.Visibility.Visible;
                SignUpBtn.IsEnabled = false;

                bool sent = await EmailService.SendOtpEmailAsync(email, otpCode);
                SignUpBtn.IsEnabled = true;
                ErrorText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                ErrorText.Visibility = System.Windows.Visibility.Collapsed;

                if (!sent)
                {
                    ShowError("Không gửi được email. Kiểm tra lại cấu hình SMTP hoặc kết nối mạng.");
                    return;
                }

                System.Windows.MessageBox.Show(
                    $"📧 Mã xác thực đã được gửi đến:\n{email}\n\nVui lòng kiểm tra hộp thư (kể cả Spam).",
                    "Email đã gửi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }

            // Mở cửa sổ nhập OTP
            var otpDialog = new VerifyOtpWindow(email, otpCode,
                showCode: !EmailService.IsConfigured) { Owner = this };

            if (otpDialog.ShowDialog() == true)
            {
                bool success = DatabaseManager.Register(email, password, name, university, studentId);
                if (!success)
                { ShowError("Email này đã được đăng ký."); return; }

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
