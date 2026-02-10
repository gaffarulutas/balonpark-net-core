using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;
using System.Text.RegularExpressions;

namespace BalonPark.Pages;

public class BlogDetailModel : BasePage
{
    private readonly IBlogService _blogService;

    public BlogDetailModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IBlogService blogService,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _blogService = blogService;
    }

    public BlogWithProducts? Blog { get; set; }
    public List<Blog> RelatedBlogs { get; set; } = new();
    public Blog? PreviousBlog { get; set; }
    public Blog? NextBlog { get; set; }
    public int ReadingTime { get; set; }
    public IUrlService? UrlService => _urlService;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            // Blog'u getir
            Blog = await _blogService.GetBlogWithProductsAsync(slug);
            
            if (Blog == null)
            {
                return NotFound();
            }

            // View count'u artır
            await _blogService.IncrementViewCountAsync(Blog.Id);

            // İlgili blogları getir
            RelatedBlogs = (await _blogService.GetRelatedBlogsAsync(Blog.Id, 3)).ToList();

            // Önceki ve sonraki blogları bul
            await FindPreviousAndNextBlogs();

            // Okuma süresini hesapla
            ReadingTime = CalculateReadingTime(Blog.Content);

            // SEO Meta Data
            ViewData["Title"] = !string.IsNullOrEmpty(Blog.MetaTitle) ? 
                Blog.MetaTitle : 
                $"{Blog.Title} - Balon Park Blog";

            ViewData["Description"] = !string.IsNullOrEmpty(Blog.MetaDescription) ? 
                Blog.MetaDescription : 
                Blog.Excerpt;

            ViewData["Keywords"] = Blog.MetaKeywords ?? 
                $"blog, {Blog.Title}, şişme oyun parkı, çocuk oyun alanı, balon park";

            ViewData["Image"] = !string.IsNullOrEmpty(Blog.FeaturedImage) ? 
                _urlService.GetImageUrl(Blog.FeaturedImage) : 
                _urlService.GetImageUrl("/assets/images/logo/logo.png");

            // JSON-LD structured data
            var structuredData = CreateStructuredData(Blog);
            ViewData["StructuredData"] = structuredData;

            return Page();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            // Log the error
            return StatusCode(500, "Blog detayı yüklenirken bir hata oluştu.");
        }
    }

    private async Task FindPreviousAndNextBlogs()
    {
        if (Blog == null) return;

        try
        {
            var allBlogs = await _blogService.GetAllBlogsAsync();
            var blogList = allBlogs.OrderBy(b => b.CreatedAt).ToList();
            
            var currentIndex = blogList.FindIndex(b => b.Id == Blog.Id);
            
            if (currentIndex > 0)
            {
                PreviousBlog = blogList[currentIndex - 1];
            }
            
            if (currentIndex < blogList.Count - 1 && currentIndex >= 0)
            {
                NextBlog = blogList[currentIndex + 1];
            }
        }
        catch
        {
            // Hata durumunda önceki/sonraki blogları bulamazsa devam et
            PreviousBlog = null;
            NextBlog = null;
        }
    }

    private int CalculateReadingTime(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 1;

        // HTML etiketlerini temizle
        var cleanContent = Regex.Replace(content, "<.*?>", " ");
        
        // Fazla boşlukları temizle
        cleanContent = Regex.Replace(cleanContent, @"\s+", " ").Trim();
        
        // Kelime sayısını hesapla (Türkçe için dakikada 200 kelime)
        var wordCount = cleanContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var readingTime = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
        
        return readingTime;
    }

    private string CreateStructuredData(BlogWithProducts blog)
    {
        var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        
        var structuredData = new
        {
            context = "https://schema.org",
            type = "BlogPosting",
            headline = blog.Title,
            description = blog.Excerpt,
            image = !string.IsNullOrEmpty(blog.FeaturedImage) ? 
                _urlService.GetImageUrl(blog.FeaturedImage) : 
                _urlService.GetImageUrl("/assets/images/logo/logo.png"),
            datePublished = blog.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? 
                           blog.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            dateModified = blog.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? 
                          blog.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            author = new
            {
                type = "Person",
                name = blog.AuthorName ?? "Balon Park",
                url = baseUrl
            },
            publisher = new
            {
                type = "Organization",
                name = "Balon Park Şişme Oyun Grupları",
                logo = new
                {
                    type = "ImageObject",
                    url = _urlService.GetImageUrl("/assets/images/logo/logo.png")
                }
            },
            mainEntityOfPage = new
            {
                type = "WebPage",
                id = $"{baseUrl}/blog/{blog.Slug}"
            },
            url = $"{baseUrl}/blog/{blog.Slug}",
            wordCount = CalculateWordCount(blog.Content),
            timeRequired = $"PT{ReadingTime}M",
            articleSection = blog.Category,
            keywords = blog.MetaKeywords,
            about = blog.RelatedProducts?.Select(p => new
            {
                type = "Product",
                name = p.Product.Name,
                description = p.Product.Description,
                image = p.MainImage != null ? 
                    _urlService.GetImageUrl(p.MainImage.ThumbnailPath) : null,
                url = $"{baseUrl}/product/{p.Product.Slug}"
            }).ToArray()
        };

        return System.Text.Json.JsonSerializer.Serialize(structuredData, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    private int CalculateWordCount(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        // HTML etiketlerini temizle
        var cleanContent = Regex.Replace(content, "<.*?>", " ");
        
        // Fazla boşlukları temizle
        cleanContent = Regex.Replace(cleanContent, @"\s+", " ").Trim();
        
        // Kelime sayısını hesapla
        return cleanContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
