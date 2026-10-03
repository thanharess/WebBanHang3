using System.Text;

namespace WebBanHang.Helpers
{
    /// <summary>Xuất CSV/TSV mở bằng Excel (UTF-8 BOM). Dùng tab để mỗi cột tách rõ.</summary>
    public static class ExcelCsv
    {
        public static byte[] Build(IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder();
            // Excel tự nhận tab-separated tốt hơn dấu phẩy (tránh gộp 1 cột)
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join("\t", row.Select(Escape)));
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
            return result;
        }

        private static string Escape(string? value)
        {
            value ??= "";
            // tab/newline → khoảng trắng để không phá cột
            value = value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ").Trim();
            if (value.Contains('"'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
