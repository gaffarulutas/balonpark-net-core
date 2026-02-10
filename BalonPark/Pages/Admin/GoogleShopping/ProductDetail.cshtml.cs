using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.GoogleShopping
{
    public class ProductDetailModel : BaseAdminPage
    {
        private readonly IGoogleShoppingService _googleShoppingService;
        private readonly CategoryRepository _categoryRepository;
        private readonly ProductImageRepository _productImageRepository;

        public ProductDetailModel(
            IGoogleShoppingService googleShoppingService,
            CategoryRepository categoryRepository,
            ProductImageRepository productImageRepository,
            IUrlService urlService,
            ICurrencyCookieService currencyCookieService)
        {
            _googleShoppingService = googleShoppingService;
            _categoryRepository = categoryRepository;
            _productImageRepository = productImageRepository;
            UrlService = urlService;
            CurrencyCookieService = currencyCookieService;
        }

        [BindProperty(SupportsGet = true)]
        public string ProductId { get; set; } = string.Empty;

        [BindProperty]
        public GoogleShoppingProduct? Product { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public bool IsSuccess { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Categories'leri yükle
                var categories = await _categoryRepository.GetAllAsync();
                Categories = categories.ToList();

                if (string.IsNullOrEmpty(ProductId))
                {
                    StatusMessage = "Ürün ID'si belirtilmedi!";
                    IsSuccess = false;
                    return;
                }

                // Google Shopping API'den ürünü getir
                var isAuthenticated = await _googleShoppingService.AuthenticateAsync();
                if (!isAuthenticated)
                {
                    StatusMessage = "Google Shopping API bağlantısı kurulamadı!";
                    IsSuccess = false;
                    return;
                }

                Product = await _googleShoppingService.GetProductAsync(ProductId);
                
                if (Product == null)
                {
                    StatusMessage = $"Ürün bulunamadı: {ProductId}";
                    IsSuccess = false;
                }
                else
                {
                    // Ürün resimlerini yükle
                    if (ProductId.StartsWith("balonpark_") && int.TryParse(ProductId.Replace("balonpark_", ""), out int productDbId))
                    {
                        var productImages = await _productImageRepository.GetByProductIdAsync(productDbId);
                        var additionalImageLinks = new List<string>();
                        
                        foreach (var image in productImages.Where(img => !img.IsMainImage).OrderBy(img => img.DisplayOrder))
                        {
                            // Resim URL'ini oluştur
                            var imageUrl = $"/{image.LargePath}";
                            additionalImageLinks.Add(imageUrl);
                        }
                        
                        Product.AdditionalImageLinks = additionalImageLinks;
                    }
                    
                    StatusMessage = "Ürün başarıyla yüklendi!";
                    IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hata: {ex.Message}";
                IsSuccess = false;
            }
        }

        public async Task<IActionResult> OnDeleteDeleteProductAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(ProductId))
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Ürün ID'si belirtilmedi!"
                    });
                }

                var success = await _googleShoppingService.DeleteProductAsync(ProductId);
                
                return new JsonResult(new
                {
                    success = success,
                    message = success ? "Ürün başarıyla silindi!" : "Ürün silinemedi!"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Ürün silme başarısız: {ex.Message}"
                });
            }
        }
    }
}
