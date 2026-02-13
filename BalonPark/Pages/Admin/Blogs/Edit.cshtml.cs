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
            // Mevcut blogu al (resim/slug vb. korumak için)
            var existingBlog = await blogRepository.GetByIdForAdminAsync(Blog.Id);
            if (existingBlog == null)
            {
                ErrorMessage = "Blog yazısı bulunamadı!";
                return RedirectToPage("Index");
            }

            // Yeni resim yüklendiyse güncelle; yoksa mevcut resmi koru (formda FeaturedImage post edilmez)
            if (Request.Form.Files.Count > 0)
            {
                var featuredImageFile = Request.Form.Files["FeaturedImageFile"];
                if (featuredImageFile != null && featuredImageFile.Length > 0)
                {
                    var imagePath = await ImageHelper.SaveBlogImageAsync(featuredImageFile);
                    Blog.FeaturedImage = imagePath;
                }
                else
                    Blog.FeaturedImage = existingBlog.FeaturedImage;
            }
            else
                Blog.FeaturedImage = existingBlog.FeaturedImage;

            // Slug oluştur (eğer boşsa)
            if (string.IsNullOrEmpty(Blog.Slug))
            {
                Blog.Slug = await blogService.GenerateSlugAsync(Blog.Title);
            }
            
            // Excerpt oluştur (eğer boşsa)
            if (string.IsNullOrEmpty(Blog.Excerpt))
            {
                Blog.Excerpt = await blogService.GenerateMetaDescriptionAsync(Blog.Content, 200);
            }

            // Meta Açıklama (SEO): özetten al, max 300 karakter
            Blog.MetaDescription = string.IsNullOrEmpty(Blog.Excerpt)
                ? string.Empty
                : (Blog.Excerpt.Length <= Blog.MetaDescriptionMaxLength ? Blog.Excerpt : Blog.Excerpt[..Blog.MetaDescriptionMaxLength]);

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
