using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using BalonPark.Data;
using BalonPark.Services;

namespace BalonPark.Pages;

/// <summary>
/// Sertifikalar sayfası: CE/EN 14960 metni ve assets/images/sertifikalar klasöründeki görseller.
/// </summary>
public class SertifikalarModel : BasePage
{
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// wwwroot/assets/images/sertifikalar içindeki görsel dosyalarının web yolu (örn. /assets/images/sertifikalar/ce.jpg).
    /// </summary>
    public List<string> CertificateImagePaths { get; private set; } = new();

    public SertifikalarModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService,
        IWebHostEnvironment env)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _env = env;
    }

    public void OnGet()
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var certFolder = Path.Combine(_env.WebRootPath ?? "", "assets", "images", "sertifikalar");

        if (!Directory.Exists(certFolder) && !string.IsNullOrEmpty(_env.ContentRootPath))
            certFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "assets", "images", "sertifikalar");

        if (Directory.Exists(certFolder))
        {
            var files = Directory
                .GetFiles(certFolder)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .Select(f => "/assets/images/sertifikalar/" + Path.GetFileName(f))
                .ToList();
            CertificateImagePaths = files;
        }
    }
}
