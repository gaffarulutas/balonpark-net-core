using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BalonPark.Services;

public interface IUrlService
{
    string GetBaseUrl();
    string GetImageUrl(string imagePath);
    string GetPageUrl(string path);
    string GetProductUrl(string categorySlug, string subCategorySlug, string productSlug);
}

public class UrlService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : IUrlService
{

    public string GetBaseUrl()
    {
        // Use the current request host whenever available so the URL is accurate
        // for whichever environment (local, staging, production) is serving.
        var request = httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            return $"{request.Scheme}://{request.Host}";
        }

        var siteUrl = configuration["siteUrl"];
        if (!string.IsNullOrEmpty(siteUrl))
        {
            return siteUrl.TrimEnd('/');
        }

        return string.Empty;
    }

    /// <summary>
    /// Resim URL'leri her zaman siteUrl (örn. https://balonpark.com) üzerinden verilir;
    /// böylece resimler tek bir domain'den yüklenir (CDN/önbellek için uygun).
    /// </summary>
    public string GetImageUrl(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return GetImageUrl("/assets/images/no-image.png");

        // Önce ImageBaseUrl, yoksa siteUrl kullan (resimler BalonPark.com'dan)
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
        if (string.IsNullOrEmpty(baseUrl))
            return path;

        // Remove leading / if present
        var cleanPath = path.TrimStart('/');
        return $"{baseUrl}/{cleanPath}";
    }

    public string GetProductUrl(string categorySlug, string subCategorySlug, string productSlug)
    {
        return GetPageUrl($"category/{categorySlug}/{subCategorySlug}/{productSlug}");
    }
}
