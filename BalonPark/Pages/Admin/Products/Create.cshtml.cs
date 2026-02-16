using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Helpers;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Products;

public class GenerateAiContentRequest
{
    public string ProductDescription { get; set; } = string.Empty;
}

public class CreateModel : BaseAdminPage
{
    private readonly ProductRepository _productRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly SubCategoryRepository _subCategoryRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly SettingsRepository _settingsRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly IAiService _aiService;
    private readonly ICacheService _cacheService;

    public CreateModel(
        ProductRepository productRepository,
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        ProductImageRepository productImageRepository,
        SettingsRepository settingsRepository,
        IWebHostEnvironment environment,
        IAiService aiService,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _productImageRepository = productImageRepository;
        _settingsRepository = settingsRepository;
        _environment = environment;
        _aiService = aiService;
        _cacheService = cacheService;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    [BindProperty]
    public List<IFormFile> Images { get; set; } = new();

    public new List<Category> Categories { get; set; } = new();
    public List<SubCategory> SubCategories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = (await _categoryRepository.GetAllAsync()).ToList();
        SubCategories = (await _subCategoryRepository.GetAllAsync()).ToList();

        // Varsayılan değerler (yeni ürün eklemede)
        Product.Stock = Product.Stock == 0 ? 1 : Product.Stock;
        Product.WarrantyDescription ??= "2 yıl garanti";
        Product.AfterSalesService ??= "5 yıllık servis ve bakım desteği";
        Product.MaterialWeightGrm2 ??= 650m;
        Product.DeliveryDaysMin ??= 3;
        Product.DeliveryDaysMax ??= 15;
        Product.FanWeightKg ??= 20m;
        Product.FanDescription ??= "1 x türbin";
        Product.RequiredPersonCount ??= 2;
        Product.AssemblyTime ??= 1m;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Product.Name))
            {
                ModelState.AddModelError("Product.Name", "Ürün adı gereklidir.");
                Categories = (await _categoryRepository.GetAllAsync()).ToList();
                SubCategories = (await _subCategoryRepository.GetAllAsync()).ToList();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Product.Description) || Product.Description.Length < 140)
            {
                ModelState.AddModelError("Product.Description", "Açıklama en az 140 karakter olmalıdır.");
                Categories = (await _categoryRepository.GetAllAsync()).ToList();
                SubCategories = (await _subCategoryRepository.GetAllAsync()).ToList();
                return Page();
            }

            if (Product.CategoryId == 0)
            {
                ModelState.AddModelError("Product.CategoryId", "Kategori seçilmelidir.");
                Categories = (await _categoryRepository.GetAllAsync()).ToList();
                SubCategories = (await _subCategoryRepository.GetAllAsync()).ToList();
                return Page();
            }

            if (Product.SubCategoryId == 0)
            {
                ModelState.AddModelError("Product.SubCategoryId", "Alt kategori seçilmelidir.");
                Categories = (await _categoryRepository.GetAllAsync()).ToList();
                SubCategories = (await _subCategoryRepository.GetAllAsync()).ToList();
                return Page();
            }

            // Slug'ı otomatik oluştur
            Product.Slug = SlugHelper.GenerateSlug(Product.Name);
            Product.CreatedAt = DateTime.Now;
            var productId = await _productRepository.CreateAsync(Product);

        // Resimleri yükle
        if (Images != null && Images.Any())
        {
            // Watermark için logo.png kullan
            var watermarkLogoPath = "/assets/images/logo/logo.png";
            
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "products", productId.ToString());
            Directory.CreateDirectory(uploadPath);

            var displayOrder = 1;
            var hasMainImage = false;
            
            foreach (var image in Images)
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
                        ProductId = productId,
                        FileName = fileName,
                        OriginalPath = $"uploads/products/{productId}/original_{fileName}",
                        LargePath = $"uploads/products/{productId}/large_{fileName}",
                        ThumbnailPath = $"uploads/products/{productId}/thumb_{fileName}",
                        IsMainImage = !hasMainImage, // Sadece ilk resim ana resim olur
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

            TempData["SuccessMessage"] = "Ürün başarıyla eklendi!";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Ürün kaydedilirken hata oluştu: {ex.Message}");
            Categories = (await _categoryRepository.GetAllAsync()).ToList();
            SubCategories = (await _subCategoryRepository.GetAllAsync()).ToList();
            return Page();
        }
    }

    public async Task<JsonResult> OnGetSubCategoriesAsync(int categoryId)
    {
        var subCategories = await _subCategoryRepository.GetByCategoryIdAsync(categoryId);
        return new JsonResult(subCategories);
    }

    public async Task<JsonResult> OnPostGenerateAiContentAsync([FromBody] GenerateAiContentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.ProductDescription))
            {
                return new JsonResult(new { success = false, message = "Ürün açıklaması boş olamaz." });
            }

            var aiResponse = await _aiService.GenerateProductContentAsync(request.ProductDescription);
            
            return new JsonResult(new 
            { 
                success = true, 
                data = new
                {
                    name = aiResponse.Name,
                    description = aiResponse.Description,
                    technicalDescription = aiResponse.TechnicalDescription,
                    summary = aiResponse.Summary,
                    suggestedPrice = aiResponse.SuggestedPrice,
                    suggestedStock = aiResponse.SuggestedStock
                }
            });
        }
        catch (Exception ex)
        {
            // Detaylı hata mesajı için log ekle
            Console.WriteLine($"AI Content Generation Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            
            return new JsonResult(new { 
                success = false, 
                message = $"Yapay zeka hatası: {ex.Message}",
                details = ex.ToString()
            });
        }
    }
}

