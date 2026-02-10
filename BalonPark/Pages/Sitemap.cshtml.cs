using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages;

public class SitemapModel(
    CategoryRepository categoryRepository,
    SubCategoryRepository subCategoryRepository,
    SettingsRepository settingsRepository,
    IUrlService urlService,
    ICurrencyCookieService currencyCookieService,
    ProductRepository productRepository,
    BlogRepository blogRepository) : BasePage(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
{
    private readonly SubCategoryRepository _subCategoryRepository = subCategoryRepository;
    private readonly ProductRepository _productRepository = productRepository;
    private readonly BlogRepository _blogRepository = blogRepository;

    public List<SubCategory> SubCategories { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<Blog> Blogs { get; set; } = [];

    public async Task OnGetAsync()
    {
        // Categories already loaded by BasePage
        
        // Get all subcategories
        var subCategoriesEnum = await _subCategoryRepository.GetAllAsync();
        SubCategories = subCategoriesEnum.OrderBy(s => s.Name).ToList();

        // Get all active products
        var productsEnum = await _productRepository.GetAllAsync();
        Products = productsEnum.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();

        // Get all published blogs
        var blogsEnum = await _blogRepository.GetAllAsync();
        Blogs = blogsEnum
            .Where(b => b.IsActive && b.PublishedAt <= DateTime.Now)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .ToList();
    }
}

