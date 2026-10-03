using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Helpers
{
    public sealed class NoWhitespaceAttribute : ValidationAttribute
    {
        public NoWhitespaceAttribute()
            : base("{0} chỉ được dùng chữ không dấu, số và ký tự đặc biệt; không được có khoảng trắng hoặc ký tự có dấu")
        {
        }

        public override bool IsValid(object? value)
        {
            return value is not string text || text.All(c => c is >= '\u0021' and <= '\u007E');
        }
    }
}
