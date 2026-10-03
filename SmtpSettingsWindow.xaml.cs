using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace soundapp
{
    public partial class SmtpSettingsWindow : Window
    {
        public SmtpSettingsWindow()
        {
            InitializeComponent();
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

            // Hiện gmail đã lưu nếu có
            try
            {
                var cred = AdysTech.CredentialManager.CredentialManager.GetCredentials("SoundStudio_SMTP");
                if (cred != null)
                {
                    GmailBox.Text = cred.UserName;
                    ShowStatus("✅ Đã có credentials được lưu. Bạn có thể cập nhật hoặc xóa.", "#4CAF50");
                }
            }
            catch { }
        }

        // Bước 1: Mở trang App Password của Google
        private void OpenGoogleBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(
                "https://myaccount.google.com/apppasswords")
            { UseShellExecute = true });
        }

        // Bước 3: Lưu vào Windows Credential Manager
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string gmail  = GmailBox.Text.Trim();
            string appPwd = AppPwdBox.Password.Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(gmail) || !gmail.Contains("@"))
            {
                ShowStatus("⚠ Vui lòng nhập địa chỉ Gmail hợp lệ.", "#ffaa00");
                return;
            }

            if (appPwd.Length < 16)
            {
                ShowStatus("⚠ App Password phải có 16 ký tự (bỏ dấu cách).", "#ffaa00");
                return;
            }

            try
            {
                EmailService.SaveCredentials(gmail, appPwd);
                ShowStatus("✅ Đã lưu an toàn! Email OTP sẽ được gửi khi đăng ký tài khoản.", "#4CAF50");
            }
            catch
            {
                ShowStatus("❌ Không thể lưu. Thử chạy app với quyền Admin.", "#ff5555");
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            EmailService.ClearCredentials();
            GmailBox.Text = "";
            AppPwdBox.Password = "";
            ShowStatus("🗑 Đã xóa credentials.", "#8783a2");
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowStatus(string msg, string color)
        {
            StatusText.Text = msg;
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
