namespace BalonPark.Services;

/// <summary>
/// Yandex Market / Yandex Direct YML (Yandex Market Language) feed üretimi.
/// 2026 best practices: tek kök öğe, tarih YYYY-MM-DD hh:mm, benzersiz ürün ID'leri (Google feed ile aynı).
/// </summary>
public interface IYandexShoppingService
{
    /// <summary>
    /// Aktif ve fiyatı olan ürünlerden YML XML feed içeriği üretir.
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>YML formatında XML string.</returns>
    Task<string> GetYmlFeedAsync(CancellationToken cancellationToken = default);
}
