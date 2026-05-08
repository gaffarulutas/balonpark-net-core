using System.Linq;

namespace BalonPark.Helpers;

/// <summary>Muhasebe formlarında seçilebilir yaygın ISO 4217 kodları (₺/$/€ ile Türkçe ad).</summary>
public static class PopularIso4217Currencies
{
    public static readonly IReadOnlyList<(string Code, string LabelTr)> Options =
    [
        ("TRY", "₺ Türk lirası · TRY"),
        ("USD", "$ Amerikan doları · USD"),
        ("EUR", "€ Euro · EUR"),
        ("GBP", "£ İngiliz sterlini · GBP"),
        ("CHF", "CHF İsviçre frangı"),
        ("JPY", "¥ Japon yeni · JPY"),
        ("SAR", "SAR Suudi riyali"),
        ("AED", "AED BAE dirhemi"),
        ("RUB", "₽ Rus rublesi · RUB"),
        ("CNY", "CNY Çin yuanı"),
        ("AUD", "A$ Avustralya doları · AUD"),
        ("CAD", "C$ Kanada doları · CAD"),
        ("DKK", "DKK Danimarka kronu"),
        ("SEK", "SEK İsveç kronu"),
        ("NOK", "NOK Norveç kronu"),
        ("PLN", "PLN Polonya zlotisi"),
        ("BGN", "BGN Bulgar levası"),
        ("RON", "RON Rumen leyi"),
        ("HUF", "HUF Macar forinti"),
        ("CZK", "CZK Çek kronu"),
        ("ILS", "₪ İsrail şekeli · ILS"),
        ("KWD", "KWD Kuveyt dinarı"),
        ("QAR", "QAR Katar riyali"),
        ("IQD", "IQD Irak dinarı"),
        ("EGP", "EGP Mısır lirası"),
    ];

    /// <summary>Seçim değerini 3 harf ISO koduna çevirir; vitrindeki TL → TRY eşlemesi.</summary>
    public static string NormalizeSelected(string? value)
    {
        var v = (value ?? "TRY").Trim().ToUpperInvariant();
        if (v == "TL")
            v = "TRY";
        return v.Length == 3 ? v : "TRY";
    }

    public static bool IsInList(string code) =>
        Options.Any(o => string.Equals(o.Code, code, StringComparison.OrdinalIgnoreCase));
}
