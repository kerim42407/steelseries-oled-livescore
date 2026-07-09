using System;
using System.Globalization;
using System.Text;

namespace OledLiveScore
{
    internal static class TextUtils
    {
        // Strip diacritics so the OLED font renders clean ASCII (Dembele <- Dembélé).
        public static string ToAscii(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (var ch in norm)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // "Ousmane Dembele" -> "O. Dembele" to fit the 21-char line.
        public static string FormatScorer(string name)
        {
            name = ToAscii(name);
            var parts = name.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return parts[0].Substring(0, 1) + ". " + parts[parts.Length - 1];
            return name;
        }

        // The OLED is 21 chars wide.
        public static string Trim21(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length > 21 ? s.Substring(0, 21) : s;
        }
    }
}
