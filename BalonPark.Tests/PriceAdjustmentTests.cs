using BalonPark.Helpers;
using Xunit;

namespace BalonPark.Tests;

public class PriceAdjustmentTests
{
    [Theory]
    [InlineData(10000, 10, false, 9000)]
    [InlineData(10000, 10, true, 11000)]
    [InlineData(2500, 10, false, 2250)]
    [InlineData(2500, 15, true, 2875)]
    [InlineData(1550, 15, true, 1783)]
    [InlineData(199, 10, true, 219)]
    [InlineData(999.99, 10, true, 1100)]
    [InlineData(10.5, 10, true, 12)]
    public void Apply_rounds_half_away_from_zero_and_drops_fraction(decimal price, int percent, bool increase, decimal expected)
    {
        Assert.Equal(expected, PriceAdjustment.Apply(price, percent, increase));
    }

    [Fact]
    public void Halfway_unit_rounds_away_from_zero()
    {
        Assert.Equal(3m, PriceAdjustment.ApplyFactor(2.5m, 1m));
        Assert.Equal(2m, PriceAdjustment.ApplyFactor(1.5m, 1m));
    }

    [Fact]
    public void Positive_price_never_falls_below_one()
    {
        Assert.Equal(1m, PriceAdjustment.Apply(1m, 90, false));
        Assert.Equal(1m, PriceAdjustment.Apply(0.4m, 50, false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-20)]
    public void Non_positive_prices_stay_unchanged(decimal price)
    {
        Assert.Equal(price, PriceAdjustment.Apply(price, 25, false));
        Assert.Equal(price, PriceAdjustment.Apply(price, 25, true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    [InlineData(-5)]
    public void Percent_outside_1_to_90_is_rejected(int percent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceAdjustment.Factor(percent, true));
    }

    [Fact]
    public void Factor_matches_the_percentage_direction()
    {
        Assert.Equal(0.90m, PriceAdjustment.Factor(10, false));
        Assert.Equal(1.15m, PriceAdjustment.Factor(15, true));
    }
}
