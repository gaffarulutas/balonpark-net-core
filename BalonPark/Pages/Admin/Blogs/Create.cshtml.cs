using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;
using BalonPark.Helpers;

namespace BalonPark.Pages.Admin.Blogs;

public class CreateModel(
    BlogRepository blogRepository,
    IBlogService blogService,
    IUrlService urlService,
    IAiService aiService,
    ICacheService cacheService)
    : BaseAdminPage
{
    [BindProperty]
    public Blog Blog { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        // UrlService'i ViewData'ya ekle
        ViewData["UrlService"] = urlService;
        
        // Varsayılan değerleri ayarla
        Blog.IsActive = true;
        Blog.IsFeatured = false;
        Blog.ViewCount = 0;
        Blog.CreatedAt = DateTime.Now;
        Blog.AuthorName = "Balon Park";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Resim yükleme işlemi
            if (Request.Form.Files.Count > 0)
            {
                var featuredImageFile = Request.Form.Files["FeaturedImageFile"];
                if (featuredImageFile != null && featuredImageFile.Length > 0)
                {
                    var imagePath = await ImageHelper.SaveBlogImageAsync(featuredImageFile);
                    Blog.FeaturedImage = imagePath;
                }
            }
            
            // Slug oluştur
            Blog.Slug = await blogService.GenerateSlugAsync(Blog.Title);
            
            // Meta description oluştur (eğer boşsa)
            if (string.IsNullOrEmpty(Blog.MetaDescription))
            {
                Blog.MetaDescription = await blogService.GenerateMetaDescriptionAsync(Blog.Content);
            }
            
            // Meta keywords oluştur (eğer boşsa)
            if (string.IsNullOrEmpty(Blog.MetaKeywords))
            {
                var keywords = await blogService.ExtractKeywordsAsync(Blog.Content);
                Blog.MetaKeywords = string.Join(", ", keywords);
            }
            
            // Meta title oluştur (eğer boşsa)
            if (string.IsNullOrEmpty(Blog.MetaTitle))
            {
                Blog.MetaTitle = Blog.Title;
            }
            
            // Excerpt oluştur (eğer boşsa)
            if (string.IsNullOrEmpty(Blog.Excerpt))
            {
                Blog.Excerpt = await blogService.GenerateMetaDescriptionAsync(Blog.Content, 200);
            }
            
            // PublishedAt ayarla
            if (Blog.IsActive)
            {
                Blog.PublishedAt = DateTime.Now;
            }
            
            // Blog'u kaydet
            var newId = await blogRepository.CreateAsync(Blog);
            
            // Cache'i temizle
            await cacheService.InvalidateBlogsAsync();
            
            SuccessMessage = "Blog yazısı başarıyla oluşturuldu!";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Blog yazısı oluşturulurken hata oluştu: {ex.Message}";
            return Page();
        }
    }

    public async Task<JsonResult> OnPostGenerateAiContentAsync([FromBody] GenerateBlogAiContentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.BlogTopic))
            {
                return new JsonResult(new { success = false, message = "Blog konusu boş olamaz." });
            }

            var aiResponse = await aiService.GenerateBlogContentAsync(request.BlogTopic);
            
            return new JsonResult(new 
            { 
                success = true, 
                data = new
                {
                    title = aiResponse.Title,
                    excerpt = aiResponse.Excerpt,
                    content = aiResponse.Content,
                    category = aiResponse.Category,
                    metaTitle = aiResponse.MetaTitle,
                    metaDescription = aiResponse.MetaDescription,
                    metaKeywords = aiResponse.MetaKeywords,
                    tags = aiResponse.Tags
                }
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { 
                success = false, 
                message = $"Yapay zeka hatası: {ex.Message}"
            });
        }
    }
}

public class GenerateBlogAiContentRequest
{
    public string BlogTopic { get; set; } = string.Empty;
}
