using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;
using BalonPark.Helpers;

namespace BalonPark.Pages.Admin.Blogs;

public class EditModel(
    BlogRepository blogRepository,
    IBlogService blogService,
    IUrlService urlService,
    ICacheService cacheService)
    : BaseAdminPage
{
    [BindProperty]
    public Blog Blog { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // UrlService'i ViewData'ya ekle
        ViewData["UrlService"] = urlService;
        
        var blog = await blogRepository.GetByIdForAdminAsync(id);
        if (blog == null)
        {
            ErrorMessage = "Blog yazısı bulunamadı!";
            return RedirectToPage("Index");
        }

        Blog = blog;
        return Page();
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
            
            // Slug oluştur (eğer boşsa)
            if (string.IsNullOrEmpty(Blog.Slug))
            {
                Blog.Slug = await blogService.GenerateSlugAsync(Blog.Title);
            }
            
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
            
            // UpdatedAt ayarla
            Blog.UpdatedAt = DateTime.Now;
            
            // PublishedAt ayarla (eğer aktifse ve henüz yayınlanmamışsa)
            if (Blog.IsActive && Blog.PublishedAt == null)
            {
                Blog.PublishedAt = DateTime.Now;
            }
            
            // Blog'u güncelle
            await blogRepository.UpdateAsync(Blog);
            
            // Cache'i temizle
            await cacheService.InvalidateBlogsAsync();
            await cacheService.InvalidateBlogAsync(Blog.Id);
            await cacheService.InvalidateBlogBySlugAsync(Blog.Slug);
            
            SuccessMessage = "Blog yazısı başarıyla güncellendi!";
            return RedirectToPage("Edit", new { id = Blog.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Blog yazısı güncellenirken hata oluştu: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostIncrementViewCountAsync()
    {
        try
        {
            await blogRepository.IncrementViewCountAsync(Blog.Id);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
}
