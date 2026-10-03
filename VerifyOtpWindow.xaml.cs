using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class VerifyOtpWindow : Window
    {
        private string _expectedOtp;

        public VerifyOtpWindow(string email, string expectedOtp, bool showCode = false)
        {
            InitializeComponent();
            _expectedOtp = expectedOtp;

            if (showCode)
                DescText.Text = $"Email chưa được cấu hình. Mã OTP để test:\n👉 {expectedOtp}";
            else
                DescText.Text = $"📧 Mã xác thực đã được gửi đến:\n{email}\n\nVui lòng kiểm tra hộp thư (kể cả Spam).";

            OtpBox.Focus();
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            if (OtpBox.Text.Trim() == _expectedOtp)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Text = "Invalid code. Please try again.";
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
