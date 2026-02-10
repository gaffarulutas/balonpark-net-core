using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Blogs;

public class IndexModel(
    BlogRepository blogRepository,
    IUrlService urlService,
    ICacheService cacheService)
    : BaseAdminPage
{
    public List<Blog> Blogs { get; set; } = [];
    
    [BindProperty(SupportsGet = true)]
    public string? TitleFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public bool? IsActiveFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public bool? IsFeaturedFilter { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        // UrlService'i ViewData'ya ekle
        ViewData["UrlService"] = urlService;
        
        // Tüm blogları getir (aktif ve pasif)
        var allBlogs = (await blogRepository.GetAllForAdminAsync()).ToList();
        
        // Filtreleme uygula
        if (!string.IsNullOrWhiteSpace(TitleFilter))
        {
            allBlogs = allBlogs.Where(b => 
                b.Title.Contains(TitleFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }
        
        if (!string.IsNullOrWhiteSpace(CategoryFilter))
        {
            allBlogs = allBlogs.Where(b => 
                b.Category != null && b.Category.Contains(CategoryFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }
        
        if (IsActiveFilter.HasValue)
        {
            allBlogs = allBlogs.Where(b => b.IsActive == IsActiveFilter.Value).ToList();
        }
        
        if (IsFeaturedFilter.HasValue)
        {
            allBlogs = allBlogs.Where(b => b.IsFeatured == IsFeaturedFilter.Value).ToList();
        }
        
        Blogs = allBlogs.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var blog = await blogRepository.GetByIdForAdminAsync(id);
            await blogRepository.DeleteAsync(id);
            
            // Cache'i temizle
            await cacheService.InvalidateBlogsAsync();
            await cacheService.InvalidateBlogAsync(id);
            if (blog != null)
            {
                await cacheService.InvalidateBlogBySlugAsync(blog.Slug);
            }
            
            SuccessMessage = "Blog yazısı başarıyla silindi!";
        }
        catch
        {
            SuccessMessage = "Blog yazısı silinirken hata oluştu!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        try
        {
            var blog = await blogRepository.GetByIdForAdminAsync(id);
            if (blog != null)
            {
                blog.IsActive = !blog.IsActive;
                await blogRepository.UpdateAsync(blog);
                
                // Cache'i temizle
                await cacheService.InvalidateBlogsAsync();
                await cacheService.InvalidateBlogAsync(id);
                await cacheService.InvalidateBlogBySlugAsync(blog.Slug);
                
                SuccessMessage = $"Blog yazısı {(blog.IsActive ? "aktif" : "pasif")} hale getirildi!";
            }
        }
        catch
        {
            SuccessMessage = "Blog yazısı durumu değiştirilirken hata oluştu!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleFeaturedAsync(int id)
    {
        try
        {
            var blog = await blogRepository.GetByIdForAdminAsync(id);
            if (blog != null)
            {
                blog.IsFeatured = !blog.IsFeatured;
                await blogRepository.UpdateAsync(blog);
                
                // Cache'i temizle
                await cacheService.InvalidateBlogsAsync();
                await cacheService.InvalidateBlogAsync(id);
                await cacheService.InvalidateBlogBySlugAsync(blog.Slug);
                
                SuccessMessage = $"Blog yazısı {(blog.IsFeatured ? "öne çıkan" : "normal")} hale getirildi!";
            }
        }
        catch
        {
            SuccessMessage = "Blog yazısı durumu değiştirilirken hata oluştu!";
        }

        return RedirectToPage();
    }
}
