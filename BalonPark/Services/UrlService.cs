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
        var siteUrl = configuration["siteUrl"];
        if (!string.IsNullOrEmpty(siteUrl))
        {
            return siteUrl.TrimEnd('/');
        }

        // Fallback to Request.Host if siteUrl is not configured
        var request = httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            return $"{request.Scheme}://{request.Host}";
        }

        return string.Empty;
    }

    public string GetImageUrl(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return GetImageUrl("/assets/images/no-image.png");

        var baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return imagePath;

        // Remove leading ~/ or / if present
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
