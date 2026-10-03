using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using AdysTech.CredentialManager;

namespace soundapp
{
    /// <summary>
    /// Gửi email OTP qua Gmail SMTP.
    /// Credentials được lưu trong Windows Credential Manager (DPAPI encrypted),
    /// KHÔNG bao giờ hardcode trong source code.
    /// </summary>
    public static class EmailService
    {
        // Key định danh trong Windows Credential Manager
        private const string CredentialKey = "SoundStudio_SMTP";
        private const string SenderName    = "Sound Studio";

        /// <summary>
        /// Kiểm tra xem credentials đã được lưu chưa.
        /// </summary>
        public static bool IsConfigured
        {
            get
            {
                try
                {
                    var cred = CredentialManager.GetCredentials(CredentialKey);
                    return cred != null
                        && !string.IsNullOrWhiteSpace(cred.UserName)
                        && cred.SecurePassword?.Length > 0;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Lưu credentials vào Windows Credential Manager (DPAPI encrypted).
        /// Gọi từ màn hình Cài đặt — KHÔNG lưu trong code.
        /// </summary>
        public static void SaveCredentials(string gmailAddress, string appPassword)
        {
            var cred = new NetworkCredential(gmailAddress, appPassword);
            CredentialManager.SaveCredentials(CredentialKey, cred);
        }

        /// <summary>
        /// Xóa credentials khỏi Windows Credential Manager.
        /// </summary>
        public static void ClearCredentials()
        {
            try { CredentialManager.RemoveCredentials(CredentialKey); } catch { }
        }

        /// <summary>
        /// Gửi email OTP xác thực tài khoản.
        /// </summary>
        public static async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
        {
            try
            {
                // Lấy credentials từ Windows Credential Manager
                var cred = CredentialManager.GetCredentials(CredentialKey);
                if (cred == null) return false;

                string senderEmail   = cred.UserName;
                string appPassword   = cred.Password;

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port        = 587,
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl   = true,
                };

                string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Segoe UI, Arial, sans-serif; background-color: #0B0914; color: white; padding: 30px;'>
    <div style='max-width: 480px; margin: auto; background: #161324; border-radius: 16px; padding: 40px; border: 1px solid #2e2a4a;'>
        <div style='text-align: center; margin-bottom: 30px;'>
            <h1 style='color: #9B4DFF; font-size: 32px; margin: 0;'>|||</h1>
            <h2 style='color: white; margin: 10px 0 5px 0;'>Sound Studio</h2>
            <p style='color: #8783a2; margin: 0; font-size: 13px;'>Feel the Difference</p>
        </div>
        <h3 style='color: white; text-align: center;'>Kích hoạt tài khoản của bạn</h3>
        <p style='color: #8783a2; text-align: center;'>Nhập mã OTP bên dưới để hoàn tất đăng ký:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <span style='background: #251b4d; color: #9B4DFF; font-size: 36px; font-weight: bold;
                         letter-spacing: 12px; padding: 15px 30px; border-radius: 10px;
                         border: 2px solid #9B4DFF;'>{otpCode}</span>
        </div>
        <p style='color: #8783a2; text-align: center; font-size: 12px;'>
            Mã này có hiệu lực trong <strong style='color: white;'>10 phút</strong>.<br/>
            Nếu bạn không đăng ký Sound Studio, hãy bỏ qua email này.
        </p>
        <hr style='border-color: #2e2a4a; margin: 30px 0;'/>
        <p style='color: #8783a2; text-align: center; font-size: 11px;'>© 2026 Sound Studio</p>
    </div>
</body>
</html>";

                var mail = new MailMessage
                {
                    From       = new MailAddress(senderEmail, SenderName),
                    Subject    = $"[Sound Studio] Mã xác thực: {otpCode}",
                    Body       = body,
                    IsBodyHtml = true,
                };
                mail.To.Add(toEmail);

                await smtpClient.SendMailAsync(mail);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
