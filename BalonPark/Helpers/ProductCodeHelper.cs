namespace BalonPark.Helpers;

public static class ProductCodeHelper
{
    public static string Format(int productId) => $"U-{productId}";

    public static bool TryParseProductId(string? query, out int productId)
    {
        productId = 0;
        if (string.IsNullOrWhiteSpace(query)) return false;

        query = query.Trim();
        if (int.TryParse(query, out productId)) return true;

        if (query.StartsWith("U-", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(query.AsSpan(2), out productId))
        {
            return true;
        }

        return false;
    }

    public static bool IsSearchableQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;
        query = query.Trim();
        if (query.Length >= 2) return true;
        return TryParseProductId(query, out _);
    }
}
