using System.Xml;
using BalonPark.Models;
using Microsoft.Extensions.Logging;

namespace BalonPark.Services;

public class CurrencyService(HttpClient httpClient)
{
    private static CurrencyResponse? _cachedCurrencies;
    private static DateTime _lastFetch = DateTime.MinValue;

    public async Task<CurrencyResponse> GetCurrenciesAsync()
    {
        // Cache 1 saat boyunca geçerli
        if (_cachedCurrencies != null && DateTime.Now - _lastFetch < TimeSpan.FromHours(1))
        {
            return _cachedCurrencies;
        }

        try
        {
            var xmlContent = await httpClient.GetStringAsync("https://www.tcmb.gov.tr/kurlar/today.xml");

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            var currencies = new List<Currency>();
            // TCMB XML formatını debug et - tüm Currency node'larını listele
            var allCurrencyNodes = xmlDoc.SelectNodes("//Currency");

            // TCMB XML formatı: Currency[@Kod='USD'] kullanıyor
            var usdNode = xmlDoc.SelectSingleNode("Tarih_Date/Currency[@Kod='USD']");
            var eurNode = xmlDoc.SelectSingleNode("Tarih_Date/Currency[@Kod='EUR']");

            if (usdNode != null)
            {
                // TCMB XML formatı: BanknoteBuying ve BanknoteSelling kullanıyor
                var usdBuyingText = usdNode.SelectSingleNode("BanknoteBuying")?.InnerText ?? "0";
                var usdSellingText = usdNode.SelectSingleNode("BanknoteSelling")?.InnerText ?? "0";

                if (decimal.TryParse(usdBuyingText.Replace(",", "."), out var usdBuying) &&
                    decimal.TryParse(usdSellingText.Replace(",", "."), out var usdSelling))
                {
                    // TCMB kurları 10000 katı olarak geliyor, 10000'e böl
                    var usdRate = (usdBuying + usdSelling) / 2 / 10000;

                    currencies.Add(new Currency
                    {
                        Code = "USD",
                        Name = usdNode.SelectSingleNode("Isim")?.InnerText ?? "ABD Doları",
                        Rate = usdRate,
                        LastUpdated = DateTime.Now
                    });
                }
            }

            if (eurNode != null)
            {
                // TCMB XML formatı: BanknoteBuying ve BanknoteSelling kullanıyor
                var eurBuyingText = eurNode.SelectSingleNode("BanknoteBuying")?.InnerText ?? "0";
                var eurSellingText = eurNode.SelectSingleNode("BanknoteSelling")?.InnerText ?? "0";

                if (decimal.TryParse(eurBuyingText.Replace(",", "."), out var eurBuying) &&
                    decimal.TryParse(eurSellingText.Replace(",", "."), out var eurSelling))
                {
                    var eurRate = (eurBuying + eurSelling) / 2 / 10000;

                    currencies.Add(new Currency
                    {
                        Code = "EUR",
                        Name = eurNode.SelectSingleNode("Isim")?.InnerText ?? "Euro",
                        Rate = eurRate,
                        LastUpdated = DateTime.Now
                    });
                }
            }

            // Eğer hiç para birimi alınamadıysa varsayılan değerleri kullan
            if (currencies.Count == 0)
            {
                currencies.AddRange(new List<Currency>
                {
                    new Currency { Code = "USD", Name = "ABD Doları", Rate = 34.50m, LastUpdated = DateTime.Now },
                    new Currency { Code = "EUR", Name = "Euro", Rate = 37.20m, LastUpdated = DateTime.Now }
                });
            }

            _cachedCurrencies = new CurrencyResponse
            {
                Currencies = currencies,
                LastUpdated = DateTime.Now
            };

            _lastFetch = DateTime.Now;

            return _cachedCurrencies;
        }
        catch (Exception)
        {
            return new CurrencyResponse
            {
                Currencies =
                [
                    new Currency { Code = "USD", Name = "ABD Doları", Rate = 34.50m, LastUpdated = DateTime.Now },
                    new Currency { Code = "EUR", Name = "Euro", Rate = 37.20m, LastUpdated = DateTime.Now }
                ],
                LastUpdated = DateTime.Now
            };
        }
    }

    public async Task<decimal> GetUsdRateAsync()
    {
        try
        {
            var currencies = await GetCurrenciesAsync();
            var usdRate = currencies.Currencies.FirstOrDefault(c => c.Code == "USD")?.Rate ?? 34.50m;
            return usdRate;
        }
        catch (Exception)
        {
            return 34.50m; // Fallback rate
        }
    }

    public async Task<decimal> GetEurRateAsync()
    {
        try
        {
            var currencies = await GetCurrenciesAsync();
            var eurRate = currencies.Currencies.FirstOrDefault(c => c.Code == "EUR")?.Rate ?? 37.20m;
            return eurRate;
        }
        catch (Exception)
        {
            return 37.20m; // Fallback rate
        }
    }

    public async Task<(decimal usdPrice, decimal euroPrice)> CalculatePricesAsync(decimal tlPrice)
    {
        try
        {
            var currencies = await GetCurrenciesAsync();
            var usdRate = currencies.Currencies.FirstOrDefault(c => c.Code == "USD")?.Rate ?? 34.50m;
            var eurRate = currencies.Currencies.FirstOrDefault(c => c.Code == "EUR")?.Rate ?? 37.20m;


            var usdPrice = tlPrice / usdRate;
            var euroPrice = tlPrice / eurRate;
            return (usdPrice, euroPrice);
        }
        catch (Exception)
        {
            // Fallback rates
            const decimal usdRate = 34.50m;
            const decimal eurRate = 37.20m;

            var usdPrice = tlPrice / usdRate;
            var euroPrice = tlPrice / eurRate;

            return (usdPrice, euroPrice);
        }
    }
}