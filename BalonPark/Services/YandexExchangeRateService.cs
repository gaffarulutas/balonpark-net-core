using System.Globalization;
using System.Xml;
using Microsoft.Extensions.Caching.Memory;

namespace BalonPark.Services;

/// <summary>
/// Yandex feed için TRY → RUB kuru. TCMB (Türkiye) + CBR (Rusya) merkez bankası kurlarından hesaplanır.
/// Önbellek: 1 saat.
/// </summary>
public class YandexExchangeRateService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    ILogger<YandexExchangeRateService> logger) : IYandexExchangeRateService
{
    private const string CacheKey = "YandexFeed:TryToRubRate";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
    /// <summary>API'ler başarısız olursa kullanılacak yaklaşık TRY/RUB (1 TRY ≈ 3 RUB).</summary>
    private const decimal FallbackTryToRubRate = 3.0m;

    public async Task<decimal> GetTryToRubRateAsync(CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(CacheKey, out decimal cached))
            return cached;

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // TCMB: 1 USD = X TRY (X = TRY per 1 USD)
            var tryPerUsd = await GetTryPerUsdAsync(client, cancellationToken);
            if (tryPerUsd <= 0)
            {
                logger.LogWarning("YandexExchangeRate: TCMB USD kuru alınamadı, fallback kullanılıyor");
                return SetCache(FallbackTryToRubRate);
            }

            // CBR: 1 USD = Y RUB (Y = RUB per 1 USD)
            var rubPerUsd = await GetRubPerUsdAsync(client, cancellationToken);
            if (rubPerUsd <= 0)
            {
                logger.LogWarning("YandexExchangeRate: CBR USD kuru alınamadı, fallback kullanılıyor");
                return SetCache(FallbackTryToRubRate);
            }

            // 1 TRY = (1 / tryPerUsd) USD = (rubPerUsd / tryPerUsd) RUB
            var tryToRub = rubPerUsd / tryPerUsd;
            if (tryToRub <= 0 || tryToRub > 100m)
            {
                logger.LogWarning("YandexExchangeRate: Hesaplanan TRY/RUB oranı anormal ({Rate}), fallback kullanılıyor", tryToRub);
                return SetCache(FallbackTryToRubRate);
            }

            return SetCache(Math.Round(tryToRub, 4));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YandexExchangeRate: Kur alınırken hata, fallback kullanılıyor");
            return SetCache(FallbackTryToRubRate);
        }
    }

    private decimal SetCache(decimal rate)
    {
        memoryCache.Set(CacheKey, rate, CacheTtl);
        return rate;
    }

    /// <summary>TCMB today.xml: USD için TRY kuru (1 USD = X TRY). ForexSelling veya BanknoteSelling; gerekirse 10000'e böl.</summary>
    private static async Task<decimal> GetTryPerUsdAsync(HttpClient client, CancellationToken ct)
    {
        var xml = await client.GetStringAsync("https://www.tcmb.gov.tr/kurlar/today.xml", ct);
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var usd = doc.SelectSingleNode("Tarih_Date/Currency[@Kod='USD']");
        if (usd == null) return 0;

        var raw = usd.SelectSingleNode("ForexSelling")?.InnerText
                  ?? usd.SelectSingleNode("BanknoteSelling")?.InnerText
                  ?? usd.SelectSingleNode("BanknoteBuying")?.InnerText
                  ?? usd.SelectSingleNode("ForexBuying")?.InnerText;
        if (string.IsNullOrWhiteSpace(raw)) return 0;

        if (!decimal.TryParse(raw.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return 0;

        // TCMB bazen oranı 10000 katı veriyor (örn. 345678 = 34.5678)
        if (value > 1000m) value /= 10000m;
        return value > 0 ? value : 0;
    }

    /// <summary>CBR XML_daily.asp: USD için RUB kuru (1 USD = Y RUB). Value virgülle ondalık.</summary>
    private static async Task<decimal> GetRubPerUsdAsync(HttpClient client, CancellationToken ct)
    {
        var xml = await client.GetStringAsync("https://www.cbr.ru/scripts/XML_daily.asp", ct);
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var nodes = doc.SelectNodes("//Valute[CharCode='USD']");
        if (nodes == null || nodes.Count == 0) return 0;

        var valueNode = nodes[0]!.SelectSingleNode("Value");
        if (valueNode?.InnerText == null) return 0;

        var raw = valueNode.InnerText.Trim().Replace(",", ".");
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return 0;

        return value > 0 ? value : 0;
    }
}
