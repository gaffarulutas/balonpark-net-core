using System.Globalization;

namespace BalonPark.Helpers;

/// <summary>
/// Admin ürün listesine dönüş adresini doğrular. Yalnızca bilinen filtre parametrelerine izin verir.
/// </summary>
public static class AdminProductListUrl
{
    public const string IndexPath = "/admin/products";
    public const int MaxLength = 2048;

    private static readonly HashSet<string> AllowedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "PageNumber",
        "SelectedCategoryIds",
        "SelectedSubCategoryIds",
        "ProductNameFilter",
        "ProductIdFilter",
        "StatusFilter",
        "SortBy"
    };

    private static readonly HashSet<string> AllowedStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        "all", "active", "inactive"
    };

    private static readonly HashSet<string> AllowedSort = new(StringComparer.OrdinalIgnoreCase)
    {
        "displayOrder", "name", "nameDesc", "price", "priceDesc", "stock", "newest", "oldest"
    };

    public static bool IsSafe(string? url, Func<string, bool> isLocalUrl)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > MaxLength)
            return false;

        if (url.Contains('#') || url.Contains('\\'))
            return false;

        if (!isLocalUrl(url))
            return false;

        var queryIndex = url.IndexOf('?');
        var path = queryIndex >= 0 ? url[..queryIndex] : url;
        if (!path.Equals(IndexPath, StringComparison.OrdinalIgnoreCase))
            return false;

        if (queryIndex < 0)
            return true;

        var query = url[(queryIndex + 1)..];
        if (query.Length == 0)
            return false;

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            var rawKey = separator >= 0 ? part[..separator] : part;
            var rawValue = separator >= 0 ? part[(separator + 1)..] : string.Empty;

            string key;
            string value;
            try
            {
                key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
                value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
            }
            catch (UriFormatException)
            {
                return false;
            }

            if (!AllowedKeys.Contains(key) || value.Any(char.IsControl))
                return false;

            if (key.Equals("PageNumber", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SelectedCategoryIds", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SelectedSubCategoryIds", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1)
                    return false;
            }
            else if (key.Equals("ProductIdFilter", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Length > 12 || value.Any(c => !char.IsDigit(c)))
                    return false;
            }
            else if (key.Equals("StatusFilter", StringComparison.OrdinalIgnoreCase))
            {
                if (!AllowedStatus.Contains(value))
                    return false;
            }
            else if (key.Equals("SortBy", StringComparison.OrdinalIgnoreCase))
            {
                if (!AllowedSort.Contains(value))
                    return false;
            }
            else if (value.Length > 200)
            {
                return false;
            }
        }

        return true;
    }
}
