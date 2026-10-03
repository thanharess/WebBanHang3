namespace WebBanHang.Services
{
    /// <summary>Cấu hình SMTP Gmail (hoặc SMTP khác).</summary>
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        /// <summary>Email Gmail gửi đi.</summary>
        public string UserName { get; set; } = "";
        /// <summary>Mật khẩu ứng dụng Gmail (App Password), không dùng mật khẩu đăng nhập thường.</summary>
        public string Password { get; set; } = "";
        public string FromName { get; set; } = "CH&T GOLF";
        public string FromEmail { get; set; } = "";
    }
}
