using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace soundapp
{
    public static class EmailService
    {
        // Cấu hình Gmail SMTP - thay bằng email và App Password của bạn
        // Tạo App Password tại: https://myaccount.google.com/apppasswords
        private const string SenderEmail = "YOUR_GMAIL@gmail.com";
        private const string SenderAppPassword = "YOUR_APP_PASSWORD"; // 16 ký tự, không phải mk Gmail
        private const string SenderName = "Sound Studio";

        public static bool IsConfigured =>
            SenderEmail != "YOUR_GMAIL@gmail.com" &&
            SenderAppPassword != "YOUR_APP_PASSWORD";

        public static async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
        {
            if (!IsConfigured)
                return false;

            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(SenderEmail, SenderAppPassword),
                    EnableSsl = true,
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
        <h3 style='color: white; text-align: center;'>Kích hoạt tài khoản</h3>
        <p style='color: #8783a2; text-align: center;'>Mã xác thực OTP của bạn là:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <span style='background: #251b4d; color: #9B4DFF; font-size: 36px; font-weight: bold;
                         letter-spacing: 12px; padding: 15px 30px; border-radius: 10px;
                         border: 2px solid #9B4DFF;'>{otpCode}</span>
        </div>
        <p style='color: #8783a2; text-align: center; font-size: 12px;'>
            Mã này có hiệu lực trong <strong style='color: white;'>10 phút</strong>.<br/>
            Nếu bạn không đăng ký tài khoản Sound Studio, hãy bỏ qua email này.
        </p>
        <hr style='border-color: #2e2a4a; margin: 30px 0;'/>
        <p style='color: #8783a2; text-align: center; font-size: 11px;'>© 2026 Sound Studio</p>
    </div>
</body>
</html>";

                var mail = new MailMessage
                {
                    From = new MailAddress(SenderEmail, SenderName),
                    Subject = $"[Sound Studio] Mã xác thực: {otpCode}",
                    Body = body,
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
