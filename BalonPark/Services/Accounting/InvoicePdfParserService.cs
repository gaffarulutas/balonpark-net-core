using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BalonPark.Models.Accounting;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace BalonPark.Services.Accounting;

/// <summary>
/// Türk e-fatura / e-arşiv fatura PDF'lerinden metin çıkarır ve verileri parse eder.
/// Farklı e-fatura sağlayıcılarının (GİB Portal, Digital Planet, Karot, Logo vb.)
/// ürettiği çeşitli formatları destekler.
/// </summary>
public sealed class InvoicePdfParserService
{
    private static readonly char[] TrimChars = [' ', '\t', '\r', '\n', ':', '：'];

    public string ExtractText(Stream pdfStream)
    {
        var sb = new StringBuilder();
        using var reader = new PdfReader(pdfStream);
        for (var page = 1; page <= reader.NumberOfPages; page++)
        {
            var pageText = PdfTextExtractor.GetTextFromPage(reader, page, new SimpleTextExtractionStrategy());
            sb.AppendLine(pageText);
        }
        return sb.ToString();
    }

    public InvoicePdfExtractResult Parse(string text, InvoiceDirection direction)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new InvoicePdfExtractResult { Success = false, ErrorMessage = "PDF'den metin çıkarılamadı." };

        var result = new InvoicePdfExtractResult { Success = true };

        ParseInvoiceNo(text, result);
        ParseIssueDate(text, result);
        ParseAmounts(text, result);
        ParseCurrency(text, result);

        if (direction == InvoiceDirection.Incoming)
            ParseSeller(text, result);
        else
            ParseBuyer(text, result);

        return result;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FATURA NO
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ParseInvoiceNo(string text, InvoicePdfExtractResult r)
    {
        // "Fatura No: GIB2026000000014" veya "Belge No: TRY2025000000009"
        var match = FirstMatch(text,
            @"Fatura\s+No\s*[:\t]+\s*([A-Z0-9][A-Z0-9\-]{2,49})",
            @"Belge\s+No\s*[:\t]+\s*([A-Z0-9][A-Z0-9\-]{2,49})");
        r.InvoiceNo = match?.Trim();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TARİH
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ParseIssueDate(string text, InvoicePdfExtractResult r)
    {
        // "Fatura Tarihi: 13-04-2026 11:34" veya "Tarihi: 30-12-2025" veya "Düzenlenme Tarihi: 30-03-2026"
        var match = FirstMatch(text,
            @"Fatura\s+Tarihi\s*[:\t]+\s*(\d{1,2}[-./]\d{1,2}[-./]\d{4})",
            @"Düzenlen?me\s+Tarihi\s*[:\t]+\s*(\d{1,2}[-./]\d{1,2}[-./]\d{4})",
            @"Düzenleme[^\d]*(\d{1,2}[-./]\d{1,2}[-./]\d{4})",
            @"Tarihi\s*[:\t]+\s*(\d{1,2}[-./]\d{1,2}[-./]\d{4})");
        if (match != null && TryParseDate(match, out var dt))
            r.IssueDate = dt;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TUTARLAR
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ParseAmounts(string text, InvoicePdfExtractResult r)
    {
        // ── Matrah (Net) ──────────────────────────────────────────────────────
        // "KDV Matrahı 23.333,33 TL" veya "Mal Hizmet Toplam Tutarı 21.363,64 TL"
        // veya "Malzeme / Hizmet Toplam Tutarı 23.207,88 TL"
        var netMatch = FirstMatch(text,
            @"KDV\s+Matrah[ıi]\s+([\d.,]+)\s*TL",
            @"Mal(?:zeme)?\s*/?\s*Hizmet\s+Toplam\s+Tutar[ıi]\s*[:\t]*\s*([\d.,]+)\s*TL");
        if (netMatch != null)
            r.AmountNet = ParseTrDecimal(netMatch);

        // ── KDV Tutarı ────────────────────────────────────────────────────────
        // "Hesaplanan KDV(%20) 4.666,67 TL"
        // "Hesaplanan KDV(%20,00) 4.641,58 TL"
        // "Hesaplanan KDV ( % 10 ) : 2.136,36 TL"
        // "Hesaplanan GERÇEK USULDE KATMA\nDEĞER VERGİSİ(45.450,00 %20) 9.090,00 TL"
        // "Vergiler Toplamı 9.090,00 TL"
        var vatMatch = FirstMatch(text,
            @"Hesaplanan\s+KDV\s*\([^)]*\)\s*[:\t]*\s*([\d.,]+)\s*TL",
            @"Hesaplanan[^(]*\([^)]*\)\s*[:\t]*\s*([\d.,]+)\s*TL",
            @"Vergiler\s+Toplam[ıi]\s*[:\t]*\s*([\d.,]+)\s*TL",
            @"Toplam\s+KDV\s+Tutar[ıi]\s*[:\t]*\s*([\d.,]+)\s*TL",
            @"KDV\s+Tutar[ıi]\s*[:\t]*\s*([\d.,]+)\s*TL");
        if (vatMatch != null)
            r.AmountVat = ParseTrDecimal(vatMatch);

        // ── Brüt tutar ───────────────────────────────────────────────────────
        // "Vergiler Dahil Toplam Tutar 28.000,00 TL" veya "Ödenecek Tutar 28.000,00 TL"
        var grossMatch = FirstMatch(text,
            @"Vergiler\s+Dahil\s+Toplam\s+Tutar\s*[:\t]*\s*([\d.,]+)\s*TL",
            @"Ödenecek\s+Tutar\s*[:\t]*\s*([\d.,]+)\s*TL");
        if (grossMatch != null)
            r.AmountGross = ParseTrDecimal(grossMatch);

        // Fallback: eğer gross = 0 ama net > 0 ise hesapla
        if (r.AmountGross == 0 && r.AmountNet > 0)
            r.AmountGross = r.AmountNet + r.AmountVat;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PARA BİRİMİ
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ParseCurrency(string text, InvoicePdfExtractResult r)
    {
        // EUR fatura tespiti: "8.000,00 EUR" veya "Ödenecek Tutar 8.000,00 EUR"
        if (Regex.IsMatch(text, @"Ödenecek\s+Tutar\s*[:\t]*\s*[\d.,]+\s*EUR", RegexOptions.IgnoreCase))
        {
            r.Currency = "EUR";
            // EUR tutarlarını yeniden parse et
            var netEur = FirstMatch(text, @"Mal(?:zeme)?\s*/?\s*Hizmet\s+Toplam\s+Tutar[ıi]\s*[:\t]*\s*([\d.,]+)\s*EUR");
            if (netEur != null) r.AmountNet = ParseTrDecimal(netEur);
            var vatEur = FirstMatch(text, @"Hesaplanan\s+KDV[^(]*\([^)]*\)\s*[:\t]*\s*([\d.,]+)\s*EUR");
            if (vatEur != null) r.AmountVat = ParseTrDecimal(vatEur);
            var grossEur = FirstMatch(text,
                @"Vergiler\s+Dahil\s+Toplam\s+Tutar\s*[:\t]*\s*([\d.,]+)\s*EUR",
                @"Ödenecek\s+Tutar\s*[:\t]*\s*([\d.,]+)\s*EUR");
            if (grossEur != null) r.AmountGross = ParseTrDecimal(grossEur);
        }
        else if (Regex.IsMatch(text, @"Ödenecek\s+Tutar\s*[:\t]*\s*[\d.,]+\s*USD", RegexOptions.IgnoreCase))
        {
            r.Currency = "USD";
        }
        else
        {
            r.Currency = "TRY";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ALICI (Buyer) — Outgoing (Kesilen) fatura: karşı taraf = alıcı
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ParseBuyer(string text, InvoicePdfExtractResult r)
    {
        // "SAYIN\n{ÜNVAN}" — e-fatura standardı
        var nameMatch = Regex.Match(text, @"SAYIN\s*[\r\n]+([^\r\n]+)", RegexOptions.IgnoreCase);
        if (nameMatch.Success)
        {
            var name = nameMatch.Groups[1].Value.Trim(TrimChars);
            // Sonraki satır da ünvanın devamı olabilir (ÖZYÜCE TURİZM....\nŞİRKETİ)
            var nextLine = Regex.Match(text, @"SAYIN\s*[\r\n]+[^\r\n]+[\r\n]+([^\r\n]+)", RegexOptions.IgnoreCase);
            if (nextLine.Success)
            {
                var nl = nextLine.Groups[1].Value.Trim();
                // Devam satırı adres değilse ve büyük harfle başlıyorsa ünvana ekle
                if (nl.Length > 2 && !IsAddressLine(nl) && char.IsUpper(nl[0]))
                    name += " " + nl;
            }
            r.CounterpartyName = name;
        }

        // VKN/TCKN: İlk eşleşme (buyer bölümünde)
        var taxId = FindFirstTaxId(text);
        if (taxId != null)
            r.CounterpartyTaxId = taxId;

        // İlk Vergi Dairesi
        var vd = FindAllTaxOffices(text);
        if (vd.Count > 0)
            r.CounterpartyTaxOffice = vd[0];

        // İlk E-Posta (boş olmayanı)
        var emails = FindAllEmails(text);
        if (emails.Count > 0)
            r.CounterpartyEmail = emails[0];

        // İlk Tel
        var phones = FindAllPhones(text);
        if (phones.Count > 0)
            r.CounterpartyPhone = phones[0];
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SATICI (Seller) — Incoming (Alınan) fatura: karşı taraf = satıcı
    // ═══════════════════════════════════════════════════════════════════════════

    private static void ParseSeller(string text, InvoicePdfExtractResult r)
    {
        // ── Vergi kimliği ─────────────────────────────────────────────────────
        var allTaxIds = FindAllTaxIds(text);
        if (allTaxIds.Count > 1)
            r.CounterpartyTaxId = allTaxIds[1]; // İkincisi satıcıya ait
        else if (allTaxIds.Count == 1)
            r.CounterpartyTaxId = allTaxIds[0];

        // ── Satıcı adı ───────────────────────────────────────────────────────
        // Strateji 1: "Mahalle/Semt:" satırının hemen öncesindeki satır
        var beforeMahalle = Regex.Match(text, @"([^\r\n]+)[\r\n]+[^\r\n]*Mahalle/Semt:", RegexOptions.IgnoreCase);
        if (beforeMahalle.Success)
        {
            var candidate = beforeMahalle.Groups[1].Value.Trim(TrimChars);
            if (IsValidName(candidate))
                r.CounterpartyName = candidate;
        }

        // Strateji 2: "İşletme Merkez:" satırının öncesindeki satır
        if (string.IsNullOrWhiteSpace(r.CounterpartyName))
        {
            var beforeMerkez = Regex.Match(text, @"([^\r\n]+)[\r\n]+İşletme\s+Merkez", RegexOptions.IgnoreCase);
            if (beforeMerkez.Success)
            {
                var candidate = beforeMerkez.Groups[1].Value.Trim(TrimChars);
                if (IsValidName(candidate))
                    r.CounterpartyName = candidate;
            }
        }

        // Strateji 3: İkinci VKN/Vergi Dairesi bloğunun öncesindeki büyük harfli satır
        if (string.IsNullOrWhiteSpace(r.CounterpartyName))
        {
            var vdMatches = Regex.Matches(text, @"Vergi\s+Dairesi\s*[:\t]+\s*([^\r\n]+)", RegexOptions.IgnoreCase);
            if (vdMatches.Count > 1)
            {
                var vdPos = vdMatches[1].Index;
                var blockBefore = text[..vdPos];
                var lines = blockBefore.Split('\n');
                for (var i = lines.Length - 1; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    if (IsValidName(line) && line.Length >= 5)
                    {
                        r.CounterpartyName = line.TrimEnd(TrimChars);
                        break;
                    }
                }
            }
        }

        // Strateji 4: Belgenin en başındaki satır (fatura-4 formatı: satıcı en üstte)
        if (string.IsNullOrWhiteSpace(r.CounterpartyName))
        {
            var firstLine = text.Split('\n').FirstOrDefault(l => l.Trim().Length >= 5)?.Trim();
            if (firstLine != null && IsValidName(firstLine) && !firstLine.StartsWith("SAYIN", StringComparison.OrdinalIgnoreCase))
                r.CounterpartyName = firstLine.TrimEnd(TrimChars);
        }

        // ── Vergi Dairesi ─────────────────────────────────────────────────────
        var vdAll = FindAllTaxOffices(text);
        if (vdAll.Count > 1)
            r.CounterpartyTaxOffice = vdAll[1]; // İkincisi satıcıya ait
        else if (vdAll.Count == 1)
            r.CounterpartyTaxOffice = vdAll[0];

        // ── E-Posta ───────────────────────────────────────────────────────────
        var emails = FindAllEmails(text);
        if (emails.Count > 1)
            r.CounterpartyEmail = emails[1];
        else if (emails.Count == 1)
            r.CounterpartyEmail = emails[0];

        // ── Telefon ───────────────────────────────────────────────────────────
        var phones = FindAllPhones(text);
        if (phones.Count > 1)
            r.CounterpartyPhone = phones[1];
        else if (phones.Count == 1)
            r.CounterpartyPhone = phones[0];
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // YARDIMCI METOTLAR
    // ═══════════════════════════════════════════════════════════════════════════

    private static string? FirstMatch(string text, params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (m.Success && m.Groups[1].Value.Trim().Length > 0)
                return m.Groups[1].Value;
        }
        return null;
    }

    private static bool TryParseDate(string s, out DateTime result)
        => DateTime.TryParseExact(s.Trim(),
            ["dd-MM-yyyy", "dd.MM.yyyy", "d-M-yyyy", "d.M.yyyy", "dd/MM/yyyy"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    private static decimal ParseTrDecimal(string s)
    {
        var normalized = s.Trim().Replace(".", "").Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    // ── Vergi numarası bulma ──────────────────────────────────────────────────

    private static string? FindFirstTaxId(string text)
    {
        var all = FindAllTaxIds(text);
        return all.Count > 0 ? all[0] : null;
    }

    private static List<string> FindAllTaxIds(string text)
    {
        // Document-order sıralaması kritik: ilk bulunan = alıcı, ikinci = satıcı
        var matches = new SortedDictionary<int, string>();

        foreach (Match m in Regex.Matches(text, @"(?:VKN|Vergi\s+No)\s*[:\t]+\s*(\d{10,11})", RegexOptions.IgnoreCase))
            matches.TryAdd(m.Index, m.Groups[1].Value.Trim());

        foreach (Match m in Regex.Matches(text, @"(?:TCKN|TC\s+Kimlik\s+No)\s*[:\t]+\s*(\d{11})", RegexOptions.IgnoreCase))
            matches.TryAdd(m.Index, m.Groups[1].Value.Trim());

        return matches.Values.ToList();
    }

    private static List<string> FindAllTaxOffices(string text)
    {
        var results = new List<string>();
        foreach (Match m in Regex.Matches(text, @"Vergi\s+Dairesi\s*[:\t]+\s*([^\r\n]+)", RegexOptions.IgnoreCase))
        {
            var val = m.Groups[1].Value.Trim(TrimChars);
            if (val.Length >= 2)
                results.Add(val);
        }
        return results;
    }

    private static List<string> FindAllEmails(string text)
    {
        var results = new List<string>();
        foreach (Match m in Regex.Matches(text, @"E-Posta\s*[:\t]+\s*([\w.%+\-]+@[\w.\-]+)", RegexOptions.IgnoreCase))
            results.Add(m.Groups[1].Value.Trim());
        return results;
    }

    private static List<string> FindAllPhones(string text)
    {
        var results = new List<string>();
        foreach (Match m in Regex.Matches(text, @"Tel\s*[:\t]+\s*([\d\s()+\-]{7,25})", RegexOptions.IgnoreCase))
        {
            var val = m.Groups[1].Value.Trim();
            if (val.Length >= 7 && !string.IsNullOrWhiteSpace(val))
                results.Add(val);
        }
        return results;
    }

    // ── İsim doğrulama ────────────────────────────────────────────────────────

    private static bool IsValidName(string s)
    {
        if (string.IsNullOrWhiteSpace(s) || s.Length < 3) return false;
        if (Regex.IsMatch(s, @"^\d+$")) return false; // Saf rakam
        if (IsAddressLine(s)) return false;
        if (s.StartsWith("Vergi", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Tel", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("E-Posta", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Web", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Mersis", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Ticaret", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("ETTN", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Fatura", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Senaryo", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Belge", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Düzenle", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Irsaliye", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("İrsaliye", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Özelleştirme", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.StartsWith("Özellestirme", StringComparison.OrdinalIgnoreCase)) return false;
        if (Regex.IsMatch(s, @"^(VKN|TCKN|TC Kimlik|SUBENO|MERSISNO|HIZMETNO|TICARETSICILNO)", RegexOptions.IgnoreCase)) return false;
        return true;
    }

    private static bool IsAddressLine(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        return Regex.IsMatch(s,
            @"(MAH[\.\s]|SOK[\.\s]|CAD[\.\s]|BULVAR|Mahalle|Cadde|Sokak|Bulvar|No\s*:|Kapı\s+No|/\s*\w+\s*/?\s*(Türkiye|Turkey)|^\d{5}\s|İÇ\s*KAPI)",
            RegexOptions.IgnoreCase);
    }
}
