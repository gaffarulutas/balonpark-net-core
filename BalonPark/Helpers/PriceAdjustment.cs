namespace BalonPark.Helpers;

/// <summary>
/// Toplu fiyat düşürme ve yükseltme. Sonuç tam para birimine yuvarlanır; kuruş kalmaz.
/// SQL tarafındaki ROUND(..., 0) ile aynı yön: yarım birimler sıfırdan uzağa yuvarlanır.
/// </summary>
public static class PriceAdjustment
{
    public const int MinPercent = 1;
    public const int MaxPercent = 90;

    public static decimal Factor(int percent, bool increase)
    {
        if (percent < MinPercent || percent > MaxPercent)
            throw new ArgumentOutOfRangeException(nameof(percent), $"Yüzde {MinPercent} ile {MaxPercent} arasında olmalıdır.");

        return increase ? 1m + (percent / 100m) : 1m - (percent / 100m);
    }

    public static decimal Apply(decimal price, int percent, bool increase)
        => ApplyFactor(price, Factor(percent, increase));

    public static decimal ApplyFactor(decimal price, decimal factor)
    {
        if (price <= 0m)
            return price;

        var adjusted = decimal.Round(price * factor, 0, MidpointRounding.AwayFromZero);
        return adjusted < 1m ? 1m : adjusted;
    }
}
