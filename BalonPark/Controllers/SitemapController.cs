using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using System.Text;
using System.Xml.Linq;

namespace BalonPark.Controllers;

/// <summary>
/// Sitemap controller for SEO optimization
/// </summary>
public class SitemapController(
    CategoryRepository categoryRepository,
    SubCategoryRepository subCategoryRepository,
    ProductRepository productRepository,
    BlogRepository blogRepository,
    IConfiguration configuration,
    ILogger<SitemapController> logger) : ControllerBase
{

    /// <summary>
    /// Generate XML sitemap
    /// </summary>
    /// <returns>XML sitemap</returns>
    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        try
        {
            var baseUrl = configuration["siteUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var sitemap = await GenerateSitemap(baseUrl);
            
            return Content(sitemap, "application/xml", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating sitemap");
            return StatusCode(500, "Error generating sitemap");
        }
    }

    /// <summary>
    /// Generate sitemap index
    /// </summary>
    /// <returns>XML sitemap index</returns>
    [HttpGet("sitemap-index.xml")]
    public IActionResult SitemapIndex()
    {
        try
        {
            var baseUrl = configuration["siteUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var sitemapIndex = GenerateSitemapIndex(baseUrl);
            
            return Content(sitemapIndex, "application/xml", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating sitemap index");
            return StatusCode(500, "Error generating sitemap index");
        }
    }

    private async Task<string> GenerateSitemap(string baseUrl)
    {
        var sitemap = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9") + "urlset",
                new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance") + "schemaLocation", 
                    "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd")
            )
        );

        var urlset = sitemap.Root!;

        // Static pages
        var staticPages = new[]
        {
            new { Url = "/", Priority = "1.0", ChangeFreq = "daily", LastMod = DateTime.Now },
            new { Url = "/Index", Priority = "1.0", ChangeFreq = "daily", LastMod = DateTime.Now },
            new { Url = "/products", Priority = "0.9", ChangeFreq = "daily", LastMod = DateTime.Now },
            new { Url = "/Blog", Priority = "0.8", ChangeFreq = "weekly", LastMod = DateTime.Now },
            new { Url = "/about", Priority = "0.8", ChangeFreq = "monthly", LastMod = DateTime.Now },
            new { Url = "/contact", Priority = "0.8", ChangeFreq = "monthly", LastMod = DateTime.Now },
            new { Url = "/certificates", Priority = "0.7", ChangeFreq = "monthly", LastMod = DateTime.Now },
            new { Url = "/privacy", Priority = "0.5", ChangeFreq = "yearly", LastMod = DateTime.Now },
            new { Url = "/terms-of-use", Priority = "0.5", ChangeFreq = "yearly", LastMod = DateTime.Now },
            new { Url = "/search", Priority = "0.6", ChangeFreq = "weekly", LastMod = DateTime.Now }
        };

        foreach (var page in staticPages)
        {
            urlset.Add(CreateUrlElement(baseUrl + page.Url, page.Priority, page.ChangeFreq, page.LastMod));
        }

        // Categories
        try
        {
            var categories = await categoryRepository.GetAllAsync();
            foreach (var category in categories.Where(c => c.IsActive && !string.IsNullOrWhiteSpace(c.Slug)))
            {
                var categoryUrl = $"{baseUrl}/category/{category.Slug}";
                urlset!.Add(CreateUrlElement(categoryUrl, "0.8", "weekly", category.UpdatedAt ?? category.CreatedAt));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error loading categories for sitemap");
        }

        // SubCategories
        try
        {
            var subCategories = await subCategoryRepository.GetAllAsync();
            foreach (var subCategory in subCategories.Where(sc => sc.IsActive 
                && !string.IsNullOrWhiteSpace(sc.CategorySlug) 
                && !string.IsNullOrWhiteSpace(sc.Slug)))
            {
                var subCategoryUrl = $"{baseUrl}/category/{subCategory.CategorySlug}/{subCategory.Slug}";
                urlset!.Add(CreateUrlElement(subCategoryUrl, "0.75", "weekly", subCategory.UpdatedAt ?? subCategory.CreatedAt));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error loading subcategories for sitemap");
        }

        // Products
        try
        {
            var products = await productRepository.GetAllAsync();
            foreach (var product in products.Where(p => p.IsActive 
                && !string.IsNullOrWhiteSpace(p.CategorySlug) 
                && !string.IsNullOrWhiteSpace(p.SubCategorySlug) 
                && !string.IsNullOrWhiteSpace(p.Slug)))
            {
                var productUrl = $"{baseUrl}/category/{product.CategorySlug}/{product.SubCategorySlug}/{product.Slug}";
                urlset!.Add(CreateUrlElement(productUrl, "0.7", "weekly", product.UpdatedAt ?? product.CreatedAt));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error loading products for sitemap");
        }

        // Blogs
        try
        {
            var blogs = await blogRepository.GetAllAsync();
            foreach (var blog in blogs.Where(b => b.IsActive && !string.IsNullOrWhiteSpace(b.Slug)))
            {
                var blogUrl = $"{baseUrl}/blog/{blog.Slug}";
                urlset!.Add(CreateUrlElement(blogUrl, "0.6", "monthly", blog.UpdatedAt ?? blog.CreatedAt));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error loading blogs for sitemap");
        }

        return sitemap.ToString();
    }

    private string GenerateSitemapIndex(string baseUrl)
    {
        var sitemapIndex = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("sitemapindex",
                new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9")
            )
        );

        var sitemapIndexRoot = sitemapIndex.Root!;

        // Main sitemap
        sitemapIndexRoot.Add(CreateSitemapElement($"{baseUrl}/sitemap.xml", DateTime.Now));

        return sitemapIndex.ToString();
    }

    private XElement CreateUrlElement(string url, string priority, string changeFreq, DateTime lastMod)
    {
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        return new XElement(ns + "url",
            new XElement(ns + "loc", url),
            new XElement(ns + "lastmod", lastMod.ToString("yyyy-MM-dd")),
            new XElement(ns + "changefreq", changeFreq),
            new XElement(ns + "priority", priority)
        );
    }

    private XElement CreateSitemapElement(string url, DateTime lastMod)
    {
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        return new XElement(ns + "sitemap",
            new XElement(ns + "loc", url),
            new XElement(ns + "lastmod", lastMod.ToString("yyyy-MM-dd"))
        );
    }
}