namespace BalonPark.Models;

public class Currency
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class CurrencyResponse
{
    public List<Currency> Currencies { get; set; } = new List<Currency>();
    public DateTime LastUpdated { get; set; }
}
