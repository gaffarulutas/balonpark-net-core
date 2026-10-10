using System.Net;
using System.Text.RegularExpressions;

namespace BalonPark.Helpers;

/// <summary>HTML metnini düz yazıya çevirip kelime sınırında kısaltır.</summary>
public static partial class TextPreview
{
    public const int SummaryLimit = 140;
    public const int DescriptionLimit = 1000;

    public readonly record struct ExpandableText(string? Html, int Limit);

    public static bool TryTruncate(string? html, int limit, out string preview)
    {
        preview = string.Empty;
        if (limit < 1)
            return false;

        var plain = ToPlainText(html);
        if (plain.Length <= limit)
            return false;

        var cut = plain[..limit];
        var lastSpace = cut.LastIndexOf(' ');
        var minKeep = (int)Math.Floor(limit * 0.6);
        if (lastSpace >= minKeep)
            cut = cut[..lastSpace];

        preview = cut.TrimEnd();
        return preview.Length > 0;
    }

    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = HtmlTagRegex().Replace(html, " ");
        text = WebUtility.HtmlDecode(text);
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
