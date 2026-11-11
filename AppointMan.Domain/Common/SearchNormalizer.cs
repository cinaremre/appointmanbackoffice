using System.Text;

namespace AppointMan.Domain.Common
{
    /// <summary>
    /// Turkce ve Latin karakterleri normalize ederek arama icin uygun hale getirir.
    /// "Kadikoy" yazan kullanici "Kadıköy" bulabilir.
    /// </summary>
    public static class SearchNormalizer
    {
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var normalized = input
                .Replace("İ", "I")
                .Replace("ı", "i")
                .Replace("Ş", "S")
                .Replace("ş", "s")
                .Replace("Ç", "C")
                .Replace("ç", "c")
                .Replace("Ğ", "G")
                .Replace("ğ", "g")
                .Replace("Ü", "U")
                .Replace("ü", "u")
                .Replace("Ö", "O")
                .Replace("ö", "o");

            normalized = normalized.ToLowerInvariant();

            var sb = new StringBuilder(normalized.Length);
            var lastWasSpace = false;

            foreach (var c in normalized)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace && sb.Length > 0)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    lastWasSpace = false;
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static bool ContainsSearch(string? searchName, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchName) || string.IsNullOrWhiteSpace(searchTerm))
                return false;

            var normalizedTerm = Normalize(searchTerm);
            return searchName.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase);
        }
    }
}