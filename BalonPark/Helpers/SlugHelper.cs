using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BalonPark.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Türkçe karakterleri değiştir
        text = text.Replace("İ", "I");
        text = text.Replace("ı", "i");
        text = text.Replace("Ğ", "G");
        text = text.Replace("ğ", "g");
        text = text.Replace("Ü", "U");
        text = text.Replace("ü", "u");
        text = text.Replace("Ş", "S");
        text = text.Replace("ş", "s");
        text = text.Replace("Ö", "O");
        text = text.Replace("ö", "o");
        text = text.Replace("Ç", "C");
        text = text.Replace("ç", "c");

        // Küçük harfe çevir
        text = text.ToLowerInvariant();

        // Özel karakterleri temizle
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // Birden fazla boşluğu tek boşluğa çevir
        text = Regex.Replace(text, @"\s+", " ").Trim();

        // Boşlukları tire ile değiştir
        text = text.Replace(" ", "-");

        // Birden fazla tire varsa tek tireye çevir
        text = Regex.Replace(text, @"-+", "-");

        return text;
    }
}

