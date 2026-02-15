using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class BlogModel : BasePage
{
    private readonly IBlogService _blogService;
    private readonly ILogger<BlogModel> _logger;

    public BlogModel(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        SettingsRepository settingsRepository,
        IBlogService blogService,
        IUrlService urlService,
        ICurrencyCookieService currencyCookieService,
        ILogger<BlogModel> logger)
        : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _blogService = blogService;
        _logger = logger;
    }

    public List<Blog> Blogs { get; set; } = new();
    public List<Blog> FeaturedBlogs { get; set; } = new();
    public List<Blog> LatestBlogs { get; set; } = new();
    public List<string> BlogCategories { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? SelectedCategory { get; set; }
    public string? SelectedTag { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalBlogs { get; set; }
    public int TotalPages { get; set; }
    public IUrlService? UrlService => _urlService;

    /// <summary>Pagination ve linkler için query string fragment (q, category, tag).</summary>
    public string GetQueryStringFragment()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(SearchQuery)) parts.Add($"q={Uri.EscapeDataString(SearchQuery)}");
        if (!string.IsNullOrEmpty(SelectedCategory)) parts.Add($"category={Uri.EscapeDataString(SelectedCategory)}");
        if (!string.IsNullOrEmpty(SelectedTag)) parts.Add($"tag={Uri.EscapeDataString(SelectedTag)}");
        return parts.Count == 0 ? "" : "&" + string.Join("&", parts);
    }

    public async Task OnGetAsync()
    {
        // Query string'den parametreleri manuel oku - HttpContext üzerinden
        var queryPage = HttpContext.Request.Query["page"].ToString();
        if (!string.IsNullOrEmpty(queryPage) && int.TryParse(queryPage, out int pageNumber))
        {
            CurrentPage = pageNumber;
        }
        else
        {
            CurrentPage = 1;
        }
        
        if (CurrentPage < 1) CurrentPage = 1;
        
        SearchQuery = HttpContext.Request.Query["q"].ToString();
        if (string.IsNullOrEmpty(SearchQuery)) SearchQuery = null;
        
        SelectedCategory = HttpContext.Request.Query["category"].ToString();
        if (string.IsNullOrEmpty(SelectedCategory)) SelectedCategory = null;

        SelectedTag = HttpContext.Request.Query["tag"].ToString();
        if (string.IsNullOrEmpty(SelectedTag)) SelectedTag = null;

        // Initialize with empty data
        Blogs = new List<Blog>();
        FeaturedBlogs = new List<Blog>();
        LatestBlogs = new List<Blog>();
        BlogCategories = new List<string>();
        
        ViewData["Title"] = "Blog - Balon Park Şişme Oyun Grupları";
        ViewData["Description"] = "Balon Park blog sayfasında şişme oyun parkları hakkında güncel yazıları keşfedin.";
        ViewData["Keywords"] = "blog, şişme oyun parkı, çocuk oyun alanı, balon park";
        ViewData["Image"] = _urlService.GetImageUrl("/assets/images/logo/logo.png");

        try
        {
            // Blog listesini getir
            IEnumerable<Blog> blogs;
            
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                blogs = await _blogService.SearchBlogsAsync(SearchQuery, 100);
                ViewData["Title"] = $"'{SearchQuery}' için arama sonuçları - Blog";
                ViewData["Description"] = $"'{SearchQuery}' ile ilgili blog yazıları. Şişme oyun parkları hakkında detaylı bilgiler.";
                ViewData["Keywords"] = $"blog arama, {SearchQuery}, şişme oyun parkı, çocuk oyun alanı";
            }
            else if (!string.IsNullOrWhiteSpace(SelectedTag))
            {
                blogs = await _blogService.GetBlogsByTagAsync(SelectedTag, 100);
                ViewData["Title"] = $"#{SelectedTag} Etiketi - Blog";
                ViewData["Description"] = $"{SelectedTag} etiketli blog yazılarımız. Şişme oyun parkları hakkında güncel bilgiler.";
                ViewData["Keywords"] = $"blog, {SelectedTag}, şişme oyun parkı, çocuk oyun alanı";
            }
            else if (!string.IsNullOrWhiteSpace(SelectedCategory))
            {
                blogs = await _blogService.GetBlogsByCategoryAsync(SelectedCategory, 100);
                ViewData["Title"] = $"{SelectedCategory} Kategorisi - Blog";
                ViewData["Description"] = $"{SelectedCategory} kategorisindeki blog yazılarımızı keşfedin. Şişme oyun parkları hakkında güncel bilgiler.";
                ViewData["Keywords"] = $"blog, {SelectedCategory}, şişme oyun parkı, çocuk oyun alanı";
            }
            else
            {
                blogs = await _blogService.GetAllBlogsAsync();
            }

            // Kategorileri getir (filtre yoksa mevcut listeden, yoksa tüm bloglardan)
            var blogsForCategories = !string.IsNullOrWhiteSpace(SearchQuery) || !string.IsNullOrWhiteSpace(SelectedCategory) || !string.IsNullOrWhiteSpace(SelectedTag)
                ? await _blogService.GetAllBlogsAsync()
                : blogs;
            BlogCategories = blogsForCategories
                .Where(b => !string.IsNullOrEmpty(b.Category))
                .Select(b => b.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Pagination
            TotalBlogs = blogs.Count();
            TotalPages = (int)Math.Ceiling((double)TotalBlogs / PageSize);
            
            Blogs = blogs
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Sidebar için öne çıkan ve son yazıları getir
            FeaturedBlogs = (await _blogService.GetFeaturedBlogsAsync(5)).ToList();
            LatestBlogs = (await _blogService.GetLatestBlogsAsync(5)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blog listesi yüklenirken hata oluştu");
        }
    }

    public async Task<IActionResult> OnGetSearchAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return new JsonResult(new { success = false, message = "Arama terimi en az 2 karakter olmalıdır." });
        }

        try
        {
            var blogs = await _blogService.SearchBlogsAsync(q, 10);
            
            var results = blogs.Select(blog => new
            {
                id = blog.Id,
                title = blog.Title,
                url = $"/blog/{blog.Slug}",
                excerpt = blog.Excerpt,
                image = !string.IsNullOrEmpty(blog.FeaturedImage) ? 
                    _urlService.GetImageUrl(blog.FeaturedImage) : 
                    _urlService.GetImageUrl("/assets/images/logo/logo.png"),
                publishedAt = blog.PublishedAt?.ToString("dd.MM.yyyy") ?? blog.CreatedAt.ToString("dd.MM.yyyy"),
                category = blog.Category
            }).ToList();

            return new JsonResult(new { success = true, results = results });
        }
        catch (Exception)
        {
            return new JsonResult(new { success = false, message = "Arama sırasında bir hata oluştu." });
        }
    }
}
