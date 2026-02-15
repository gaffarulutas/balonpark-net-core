namespace BalonPark.Services;

/// <summary>
/// Yandex feed için TRY → RUB kuru. TCMB (Türkiye) + CBR (Rusya) merkez bankası kurlarından hesaplanır.
/// </summary>
public interface IYandexExchangeRateService
{
    /// <summary>
    /// 1 TRY = ? RUB çarpanı. Fiyat RUB = fiyat TRY * bu değer.
    /// Önbelleğe alınır (varsayılan 1 saat).
    /// </summary>
    Task<decimal> GetTryToRubRateAsync(CancellationToken cancellationToken = default);
}
