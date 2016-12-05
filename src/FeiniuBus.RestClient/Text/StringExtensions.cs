using System.Net;
using System.Text;

namespace FeiniuBus.RestClient.Text
{
    public static class StringExtensions
    {
        private const int LowerCaseOffset = 'a' - 'A';

        public static string UrlEncode(this string text)
        {
            return WebUtility.UrlEncode(text);
        }

        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var len = value.Length;
            var newValue = new char[len];
            var firstPart = true;

            for (var i = 0; i < len; i++)
            {
                var c0 = value[i];
                var c1 = i < len - 1 ? value[i + 1] : 'A';
                var c0IsUpper = (c0 >= 'A') && (c0 <= 'Z');
                var c1IsUpper = (c1 >= 'A') && (c1 <= 'Z');

                if (firstPart && c0IsUpper && (c1IsUpper || (i == 0)))
                    c0 = (char)(c0 + LowerCaseOffset);
                else
                    firstPart = false;

                newValue[i] = c0;
            }

            return new string(newValue);
        }

        public static string ToLowercaseUnderscore(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            value = value.ToCamelCase();

            var sb = new StringBuilder();
            foreach (var t in value)
                if (char.IsDigit(t) || (char.IsLetter(t) && char.IsLower(t)) || (t == '_'))
                {
                    sb.Append(t);
                }
                else
                {
                    sb.Append("_");
                    sb.Append(char.ToLowerInvariant(t));
                }

            return sb.ToString();
        }
    }
}
