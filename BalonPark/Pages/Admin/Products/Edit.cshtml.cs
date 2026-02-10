using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Products;

public class EditModel : BaseAdminPage
{
    private readonly ProductRepository _productRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly SettingsRepository _settingsRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ICacheService _cacheService;

    public EditModel(
        ProductRepository productRepository,
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        ProductImageRepository productImageRepository,
        SettingsRepository settingsRepository,
        IWebHostEnvironment environment,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _productImageRepository = productImageRepository;
        _settingsRepository = settingsRepository;
        _environment = environment;
        _cacheService = cacheService;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    [BindProperty]
    public List<IFormFile> NewImages { get; set; } = new();

    public new List<Category> Categories { get; set; } = new();
    public List<SubCategory> SubCategories { get; set; } = new();
    public List<ProductImage> ProductImages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        
        if (product == null)
        {
            return RedirectToPage("./Index");
        }

        Product = product;
        Categories = (await _categoryRepository.GetAllAsync()).ToList();
        
        // Sadece seçili kategoriye ait alt kategorileri yükle
        var allSubCategories = await _subCategoryRepository.GetAllAsync();
        SubCategories = allSubCategories
            .Where(sc => sc.CategoryId == Product.CategoryId)
            .ToList();
            
        ProductImages = (await _productImageRepository.GetByProductIdAsync(id)).ToList();
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Product.Name))
        {
            ModelState.AddModelError("Product.Name", "Ürün adı gereklidir.");
            Categories = (await _categoryRepository.GetAllAsync()).ToList();
            SubCategories = (await _subCategoryRepository.GetAllAsync()).ToList();
            ProductImages = (await _productImageRepository.GetByProductIdAsync(Product.Id)).ToList();
            return Page();
        }

        // Slug'ı otomatik güncelle
        Product.Slug = SlugHelper.GenerateSlug(Product.Name);
        Product.UpdatedAt = DateTime.Now;
        await _productRepository.UpdateAsync(Product);

        // Yeni resimleri yükle
        if (NewImages != null && NewImages.Any())
        {
            // Watermark için logo.png'yi kullan
            var watermarkLogoPath = "/assets/images/logo/logo.png";
            
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "products", Product.Id.ToString());
            Directory.CreateDirectory(uploadPath);

            var existingImages = await _productImageRepository.GetByProductIdAsync(Product.Id);
            var displayOrder = existingImages.Any() ? existingImages.Max(x => x.DisplayOrder) + 1 : 1;
            var hasMainImage = existingImages.Any(x => x.IsMainImage);

            foreach (var image in NewImages)
            {
                if (image.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    
                    // Watermark ile kaydet (logo varsa)
                    (string originalPath, string largePath, string thumbnailPath) = await ImageHelper.SaveProductImageWithWatermarkAsync(
                        image,
                        uploadPath,
                        fileName,
                        watermarkLogoPath,
                        watermarkOpacity: 0.3f,     // %30 saydam (çok az görünür)
                        watermarkScale: 0.1f       // Resmin %10'u kadar (küçük)
                    );

                    var productImage = new ProductImage
                    {
                        ProductId = Product.Id,
                        FileName = fileName,
                        OriginalPath = $"uploads/products/{Product.Id}/original_{fileName}",
                        LargePath = $"uploads/products/{Product.Id}/large_{fileName}",
                        ThumbnailPath = $"uploads/products/{Product.Id}/thumb_{fileName}",
                        IsMainImage = !hasMainImage, // Sadece ana resim yoksa ilk yeni resim ana resim olur
                        DisplayOrder = displayOrder,
                        CreatedAt = DateTime.Now
                    };

                    await _productImageRepository.CreateAsync(productImage);
                    
                    if (!hasMainImage)
                    {
                        hasMainImage = true;
                    }
                    
                    displayOrder++;
                }
            }
        }

        // Cache'i temizle
        await _cacheService.InvalidateProductsAsync();
        await _cacheService.InvalidateProductAsync(Product.Id);
        await _cacheService.InvalidateProductBySlugAsync(Product.Slug);

        TempData["SuccessMessage"] = "Ürün başarıyla güncellendi!";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(int imageId, int productId)
    {
        var image = await _productImageRepository.GetByIdAsync(imageId);
        if (image != null)
        {
            var basePath = Path.Combine(_environment.WebRootPath);
            ImageHelper.DeleteProductImages(
                Path.Combine(basePath, image.OriginalPath),
                Path.Combine(basePath, image.LargePath),
                Path.Combine(basePath, image.ThumbnailPath)
            );
            
            await _productImageRepository.DeleteAsync(imageId);
            
            // Cache'i temizle
            await _cacheService.InvalidateProductsAsync();
            await _cacheService.InvalidateProductAsync(productId);
        }

        return RedirectToPage(new { id = productId });
    }

    public async Task<IActionResult> OnPostSetMainImageAsync(int imageId, int productId)
    {
        try
        {
            var result = await _productImageRepository.SetMainImageAsync(productId, imageId);
            
            if (result > 0)
            {
                // Cache'i temizle
                await _cacheService.InvalidateProductsAsync();
                await _cacheService.InvalidateProductAsync(productId);
                
                TempData["SuccessMessage"] = "Ana resim başarıyla güncellendi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Ana resim güncellenirken bir hata oluştu!";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Hata: {ex.Message}";
        }
        
        return RedirectToPage(new { id = productId });
    }


    public async Task<JsonResult> OnGetSubCategoriesAsync(int categoryId)
    {
        var subCategories = await _subCategoryRepository.GetByCategoryIdAsync(categoryId);
        return new JsonResult(subCategories);
    }
}

