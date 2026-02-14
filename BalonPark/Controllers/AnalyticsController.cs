using Microsoft.AspNetCore.Mvc;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Controllers;

/// <summary>
/// Admin dashboard için Google Analytics raporları (anlık, veritabanına kaydetmez).
/// Sadece giriş yapmış admin erişebilir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(
    IGoogleAnalyticsService analyticsService,
    ILogger<AnalyticsController> logger) : ControllerBase
{
    /// <summary>
    /// Dashboard için tüm GA raporlarını döner. Admin oturumu gerekir. refresh=true ile önbellek atlanır.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
    {
        if (HttpContext.Session == null || !HttpContext.Session.IsAdminLoggedIn())
        {
            return Unauthorized(new { success = false, message = "Oturum açmanız gerekiyor." });
        }

        try
        {
            var data = await analyticsService.GetDashboardAsync(skipCache: refresh, cancellationToken);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Analytics dashboard yüklenemedi");
            return BadRequest(new { success = false, message = "Raporlar yüklenirken hata oluştu.", data = (object?)null });
        }
    }
}
