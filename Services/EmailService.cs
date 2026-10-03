using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WebBanHang.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.UserName)
            && !string.IsNullOrWhiteSpace(_settings.Password);

        public async Task SendAsync(string toEmail, string subject, string bodyHtml)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình Gmail SMTP. Hãy đặt Email__UserName và Email__Password " +
                    "bằng User Secrets hoặc biến môi trường; không lưu mật khẩu trong source code.");
            }

            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? _settings.UserName : _settings.FromEmail;

            using var message = new MailMessage
            {
                From = new MailAddress(from, _settings.FromName),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}
