using Microsoft.Extensions.Configuration;

namespace BalonPark.Services;

public interface IUrlService
{
    string GetBaseUrl();
    string GetImageUrl(string imagePath);
    string GetPageUrl(string path);
    string GetProductUrl(string categorySlug, string subCategorySlug, string productSlug);
}

public class UrlService(IConfiguration configuration) : IUrlService
{
    public const string CanonicalBaseUrl = "https://balonpark.com";

    public string GetBaseUrl() => CanonicalBaseUrl;

    /// <summary>
    /// Resim URL'leri her zaman siteUrl (örn. https://balonpark.com) üzerinden verilir;
    /// böylece resimler tek bir domain'den yüklenir (CDN/önbellek için uygun).
    /// </summary>
    public string GetImageUrl(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return GetImageUrl("/assets/images/no-image.png");

        var baseUrl = configuration["ImageBaseUrl"]?.TrimEnd('/')
            ?? configuration["siteUrl"]?.TrimEnd('/')
            ?? GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return imagePath;

        var cleanPath = imagePath.TrimStart('~', '/');
        return $"{baseUrl}/{cleanPath}";
    }

    public string GetPageUrl(string path)
    {
        var baseUrl = GetBaseUrl();
        var cleanPath = path.TrimStart('/');
        return $"{baseUrl}/{cleanPath}";
    }

    public string GetProductUrl(string categorySlug, string subCategorySlug, string productSlug)
    {
        return GetPageUrl($"category/{categorySlug}/{subCategorySlug}/{productSlug}");
    }
}
