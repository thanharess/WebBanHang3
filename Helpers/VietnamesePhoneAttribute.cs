using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebBanHang.Helpers
{
    /// <summary>SĐT di động VN: 0xx / +84xx / 84xx (10 số sau chuẩn hoá).</summary>
    public class VietnamesePhoneAttribute : ValidationAttribute
    {
        private static readonly Regex Pattern = new(
            @"^0(3[2-9]|5[6-9]|7[06-9]|8[1-9]|9\d)\d{7}$",
            RegexOptions.Compiled);

        public VietnamesePhoneAttribute()
            : base("Số điện thoại không hợp lệ. Ví dụ: 0912345678, 84912345678 hoặc +84912345678.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            return Pattern.IsMatch(Normalize(value.ToString()));
        }

        public static string Normalize(string? input)
        {
            var raw = (input ?? "").Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
            if (raw.StartsWith("+84")) raw = "0" + raw[3..];
            else if (raw.StartsWith("84") && raw.Length >= 11) raw = "0" + raw[2..];
            return raw;
        }
    }
}
