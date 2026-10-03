using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

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
                if (cred != null) GmailBox.Text = cred.UserName;
            }
            catch { }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string gmail = GmailBox.Text.Trim();
            string appPwd = AppPwdBox.Password.Trim().Replace(" ", "");

            if (string.IsNullOrWhiteSpace(gmail) || !gmail.Contains("@"))
            { ShowStatus("Vui lòng nhập địa chỉ Gmail hợp lệ.", "#ff5555"); return; }

            if (appPwd.Length < 16)
            { ShowStatus("App Password phải có 16 ký tự (bỏ dấu cách).", "#ff5555"); return; }

            try
            {
                EmailService.SaveCredentials(gmail, appPwd);
                ShowStatus("✅ Đã lưu an toàn vào Windows Credential Manager!", "#4CAF50");
            }
            catch
            {
                ShowStatus("❌ Lỗi khi lưu credentials.", "#ff5555");
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void ShowStatus(string msg, string color)
        {
            StatusText.Text = msg;
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
