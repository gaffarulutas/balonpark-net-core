using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Services;

namespace BalonPark.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : BasePage
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>
    /// Development ortamında true; Production'da kullanıcıya farklı mesaj gösterilir.
    /// </summary>
    public bool IsDevelopment { get; set; }

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(
        ILogger<ErrorModel> logger,
        IWebHostEnvironment env,
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _logger = logger;
        IsDevelopment = env.IsDevelopment();
    }

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        _logger.LogError(
            "Hata sayfası görüntülendi. RequestId: {RequestId}, Path: {Path}",
            RequestId,
            HttpContext.Request.Path);
    }
}