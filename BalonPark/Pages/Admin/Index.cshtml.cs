using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin;

public class IndexModel : BaseAdminPage
{
    private readonly CategoryRepository _categoryRepository;
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly ProductRepository _productRepository;
    private readonly BlogRepository _blogRepository;
    private readonly IMailService _mailService;

    public IndexModel(
        CategoryRepository categoryRepository, 
        SubCategoryRepository subCategoryRepository,
        ProductRepository productRepository,
        BlogRepository blogRepository,
        IMailService mailService)
    {
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _productRepository = productRepository;
        _blogRepository = blogRepository;
        _mailService = mailService;
    }

    // Product Statistics
    public int TotalCategories { get; set; }
    public int TotalSubCategories { get; set; }
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    
    // Financial
    public decimal TotalInventoryValue { get; set; }
    public decimal AverageProductPrice { get; set; }
    
    // Blog Statistics
    public int TotalBlogs { get; set; }
    public int ActiveBlogs { get; set; }
    public int FeaturedBlogs { get; set; }
    public int TotalBlogViews { get; set; }
    
    // Email Statistics
    public int TotalInboxEmails { get; set; }
    public int UnreadEmails { get; set; }
    public int FlaggedEmails { get; set; }
    
    // Recent Activity
    public List<Product> RecentlyAddedProducts { get; set; } = new();
    public List<Product> LowStockProductsList { get; set; } = new();
    public List<Category> TopCategories { get; set; } = new();
    public List<Blog> RecentBlogs { get; set; } = new();

    public async Task OnGetAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        var subCategories = await _subCategoryRepository.GetAllAsync();
        var products = (await _productRepository.GetAllAsync()).ToList();

        // Basic Product Statistics
        TotalCategories = categories.Count();
        TotalSubCategories = subCategories.Count();
        TotalProducts = products.Count;
        ActiveProducts = products.Count(p => p.IsActive);
        InactiveProducts = products.Count(p => !p.IsActive);
        LowStockProducts = products.Count(p => p.Stock < 1);
        OutOfStockProducts = products.Count(p => p.Stock == 0);
        
        // Financial Calculations
        TotalInventoryValue = products.Sum(p => p.Price * p.Stock);
        AverageProductPrice = products.Any() ? products.Average(p => p.Price) : 0;
        
        // Blog Statistics
        var blogs = (await _blogRepository.GetAllForAdminAsync()).ToList();
        TotalBlogs = blogs.Count;
        ActiveBlogs = blogs.Count(b => b.IsActive);
        FeaturedBlogs = blogs.Count(b => b.IsFeatured);
        TotalBlogViews = blogs.Sum(b => b.ViewCount);
        
        // Email Statistics
        try
        {
            var emailStats = await _mailService.GetEmailStatsAsync();
            TotalInboxEmails = emailStats.TotalInbox;
            UnreadEmails = emailStats.UnreadCount;
            FlaggedEmails = emailStats.FlaggedCount;
        }
        catch
        {
            // Email servisi hata verirse varsayılan değerler kullan
            TotalInboxEmails = 0;
            UnreadEmails = 0;
            FlaggedEmails = 0;
        }
        
        // Recent Activity
        RecentlyAddedProducts = products
            .OrderByDescending(p => p.Id)
            .Take(5)
            .ToList();
            
        LowStockProductsList = products
            .Where(p => p.Stock < 1)
            .OrderBy(p => p.Stock)
            .Take(5)
            .ToList();
            
        TopCategories = categories
            .Select(c => new
            {
                Category = c,
                ProductCount = products.Count(p => p.CategoryId == c.Id)
            })
            .OrderByDescending(x => x.ProductCount)
            .Take(5)
            .Select(x => x.Category)
            .ToList();
            
        // Recent Blogs
        RecentBlogs = blogs
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToList();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Admin/Login");
    }
}

