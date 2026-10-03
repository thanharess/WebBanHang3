using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace WebBanHang.Services
{
    public sealed class CaptchaService
    {
        public const string SessionKey = "AccountCaptcha";
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private static readonly char[] NoiseCharacters = { '.', '+', '*', '#', '~' };

        public string Create(HttpContext context)
        {
            var code = string.Create(5, this, static (chars, service) =>
            {
                for (var i = 0; i < chars.Length; i++)
                {
                    chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
                }
            });
            context.Session.SetString(SessionKey, code);
            return code;
        }

        public bool ValidateAndConsume(HttpContext context, string? submittedCode)
        {
            var expected = context.Session.GetString(SessionKey);
            context.Session.Remove(SessionKey);
            return !string.IsNullOrWhiteSpace(expected)
                && string.Equals(expected, submittedCode?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public byte[] RenderSvg(string code)
        {
            var random = new Random(HashCode.Combine(code, DateTime.UtcNow.Ticks));
            var builder = new StringBuilder();
            builder.Append("<svg xmlns='http://www.w3.org/2000/svg' width='190' height='64' viewBox='0 0 190 64'>");
            builder.Append("<rect width='190' height='64' rx='8' fill='#f1f5f9'/>");

            for (var i = 0; i < 8; i++)
            {
                var x1 = random.Next(0, 190);
                var y1 = random.Next(8, 58);
                var x2 = random.Next(0, 190);
                var y2 = random.Next(8, 58);
                builder.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='#94a3b8' stroke-width='1'/>");
            }

            for (var i = 0; i < code.Length; i++)
            {
                var x = 18 + i * 33;
                var y = 43 + random.Next(-4, 5);
                var rotation = random.Next(-18, 19);
                var color = i % 2 == 0 ? "#1d4ed8" : "#b91c1c";
                builder.Append($"<text x='{x}' y='{y}' fill='{color}' font-family='Arial,sans-serif' font-size='29' font-weight='700' transform='rotate({rotation} {x} {y})'>{code[i]}</text>");
            }

            for (var i = 0; i < 16; i++)
            {
                var x = random.Next(4, 186);
                var y = random.Next(5, 60);
                builder.Append($"<text x='{x}' y='{y}' fill='#64748b' font-size='10'>{NoiseCharacters[random.Next(NoiseCharacters.Length)]}</text>");
            }

            builder.Append("</svg>");
            return Encoding.UTF8.GetBytes(builder.ToString());
        }
    }
}
