using Microsoft.AspNetCore.Mvc;
using BalonPark.Services;

namespace BalonPark.Controllers;

/// <summary>
/// Yandex Market / Yandex Direct YML feed endpoint.
/// Public URL: /feed/yandex-market.xml (Yandex 2026 best practices).
/// </summary>
[ApiController]
[Route("feed")]
[ResponseCache(Duration = 900, Location = ResponseCacheLocation.Any)]
public class YandexShoppingController(IYandexShoppingService yandexShoppingService, ILogger<YandexShoppingController> logger) : ControllerBase
{
    /// <summary>
    /// Yandex Market YML feed. Yandex Merchant / Yandex Direct'te bu URL'yi feed adresi olarak ekleyin.
    /// Content-Type: application/xml (Yandex gereksinimleri).
    /// </summary>
    [HttpGet("yandex-market.xml")]
    [Produces("application/xml", "text/xml")]
    public async Task<IActionResult> GetYmlFeed(CancellationToken cancellationToken)
    {
        try
        {
            var xml = await yandexShoppingService.GetYmlFeedAsync(cancellationToken);
            return Content(xml, "application/xml; charset=utf-8");
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yandex YML feed oluşturulurken hata");
            return StatusCode(500, "Feed oluşturulamadı.");
        }
    }
}
