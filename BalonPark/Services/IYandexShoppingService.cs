namespace BalonPark.Services;

/// <summary>
/// Yandex Market / Yandex Direct feed üretimi.
/// İki format: YML (yandex-market.xml) ve RSS 2.0 + g: namespace (yandex-merchant-center.xml, ücretsiz listeleme).
/// </summary>
public interface IYandexShoppingService
{
    /// <summary>
    /// Aktif ve fiyatı olan ürünlerden YML XML feed içeriği üretir (Yandex Market).
    /// </summary>
    Task<string> GetYmlFeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Yandex Merchant Center / Alışveriş ücretsiz listeleme için RSS 2.0 feed (g: namespace, TL, g:shipping).
    /// </summary>
    Task<string> GetMerchantCenterRssFeedAsync(CancellationToken cancellationToken = default);
}
