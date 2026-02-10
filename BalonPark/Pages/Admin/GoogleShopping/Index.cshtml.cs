using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.GoogleShopping
{
    public class IndexModel : BaseAdminPage
    {
        private readonly IGoogleShoppingService _googleShoppingService;
        private readonly ProductRepository _productRepository;
        private readonly CategoryRepository _categoryRepository;

        public IndexModel(
            IGoogleShoppingService googleShoppingService,
            ProductRepository productRepository,
            CategoryRepository categoryRepository,
            IUrlService urlService,
            ICurrencyCookieService currencyCookieService)
        {
            _googleShoppingService = googleShoppingService;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            UrlService = urlService;
            CurrencyCookieService = currencyCookieService;
        }

        [BindProperty]
        public bool IsAuthenticated { get; set; }

        [BindProperty]
        public int TotalProducts { get; set; }

        [BindProperty]
        public List<GoogleShoppingProduct> GoogleProducts { get; set; } = new();

        [BindProperty]
        public List<GoogleShoppingProduct> PendingProducts { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> ApprovalStatus { get; set; } = new();

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
                
                IsAuthenticated = await _googleShoppingService.AuthenticateAsync();
                
                var products = await _productRepository.GetAllForGoogleShoppingAsync();
                TotalProducts = products.Count();
                
                // Onaya gönderilecek ürünleri yükle
                PendingProducts = await _googleShoppingService.GetProductsForApprovalAsync();
                
                // Google'daki mevcut ürünleri yükle
                if (IsAuthenticated)
                {
                    GoogleProducts = await _googleShoppingService.GetAllProductsAsync();
                    ApprovalStatus = await _googleShoppingService.GetProductApprovalStatusAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hata: {ex.Message}";
                IsSuccess = false;
            }
        }

        public async Task<IActionResult> OnPostTestConnectionAsync()
        {
            try
            {
                var isAuthenticated = await _googleShoppingService.AuthenticateAsync();
                
                if (isAuthenticated)
                {
                    // Merchant hesabına erişim testi
                    try
                    {
                        var products = await _googleShoppingService.GetAllProductsAsync();
                        
                        return new JsonResult(new
                        {
                            success = true,
                            message = $"Google Shopping API bağlantısı başarılı! Merchant hesabında {products.Count} ürün bulundu.",
                            merchantAccess = true,
                            productCount = products.Count
                        });
                    }
                    catch (Exception merchantEx)
                    {
                        return new JsonResult(new
                        {
                            success = false,
                            message = $"API bağlantısı başarılı ama Merchant hesabına erişim reddedildi: {merchantEx.Message}",
                            merchantAccess = false,
                            error = merchantEx.Message
                        });
                    }
                }
                else
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Google Shopping API bağlantısı başarısız!"
                    });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Bağlantı testi başarısız: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnPostSyncAllProductsAsync()
        {
            try
            {
                var isAuthenticated = await _googleShoppingService.AuthenticateAsync();
                if (!isAuthenticated)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Google Shopping API bağlantısı kurulamadı!"
                    });
                }

                // Tüm ürünleri veritabanından Google Shopping formatına çevir
                var allProducts = await _googleShoppingService.ConvertProductsToGoogleShoppingFormatAsync();
                
                if (allProducts.Count == 0)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Güncellenecek ürün bulunamadı!"
                    });
                }

                // Mevcut Google ürünlerini kontrol et
                var existingProducts = await _googleShoppingService.GetAllProductsAsync();
                var existingProductIds = existingProducts.Select(p => p.Id).ToHashSet();

                var productsToUpdate = new List<GoogleShoppingProduct>();
                var productsToInsert = new List<GoogleShoppingProduct>();

                foreach (var product in allProducts)
                {
                    if (existingProductIds.Contains(product.Id))
                    {
                        productsToUpdate.Add(product);
                    }
                    else
                    {
                        productsToInsert.Add(product);
                    }
                }

                var updateCount = 0;
                var insertCount = 0;

                // Mevcut ürünleri güncelle
                if (productsToUpdate.Count > 0)
                {
                    var updateSuccess = await _googleShoppingService.BatchUpdateProductsAsync(productsToUpdate);
                    if (updateSuccess)
                    {
                        updateCount = productsToUpdate.Count;
                    }
                }

                // Yeni ürünleri ekle
                if (productsToInsert.Count > 0)
                {
                    var insertSuccess = await _googleShoppingService.BatchInsertProductsAsync(productsToInsert);
                    if (insertSuccess)
                    {
                        insertCount = productsToInsert.Count;
                    }
                }

                var totalProcessed = updateCount + insertCount;
                
                return new JsonResult(new
                {
                    success = true,
                    message = $"Senkronizasyon tamamlandı!\n\n📊 İşlem Özeti:\n• Güncellenen: {updateCount} ürün\n• Eklenen: {insertCount} ürün\n• Toplam: {totalProcessed} ürün",
                    updateCount = updateCount,
                    insertCount = insertCount,
                    totalCount = totalProcessed
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Senkronizasyon başarısız: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnPostViewGoogleProductsAsync()
        {
            try
            {
                var isAuthenticated = await _googleShoppingService.AuthenticateAsync();
                if (!isAuthenticated)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Google Shopping API bağlantısı kurulamadı!"
                    });
                }

                GoogleProducts = await _googleShoppingService.GetAllProductsAsync();
                
                return new JsonResult(new
                {
                    success = true,
                    message = $"{GoogleProducts.Count} ürün Google'dan başarıyla yüklendi!"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Google ürünleri yüklenemedi: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnPostExportProductsAsync()
        {
            try
            {
                var googleProducts = await _googleShoppingService.ConvertProductsToGoogleShoppingFormatAsync();
                var json = System.Text.Json.JsonSerializer.Serialize(googleProducts, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                return new FileContentResult(System.Text.Encoding.UTF8.GetBytes(json), "application/json")
                {
                    FileDownloadName = $"google-shopping-products-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json"
                };
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Dışa aktarma başarısız: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnDeleteDeleteProductAsync(string productId)
        {
            try
            {
                var success = await _googleShoppingService.DeleteProductAsync(productId);
                
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

        public async Task<IActionResult> OnPostSubmitForApprovalAsync()
        {
            try
            {
                var isAuthenticated = await _googleShoppingService.AuthenticateAsync();
                if (!isAuthenticated)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Google Shopping API bağlantısı kurulamadı!"
                    });
                }

                var pendingProducts = await _googleShoppingService.GetProductsForApprovalAsync();
                var success = await _googleShoppingService.SubmitProductsForApprovalAsync(pendingProducts);
                
                return new JsonResult(new
                {
                    success = success,
                    message = success ? 
                        $"{pendingProducts.Count} ürün onaya başarıyla gönderildi! Google 24-48 saat içinde işleyecek." : 
                        "Ürünler onaya gönderilemedi!",
                    productCount = pendingProducts.Count
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Onaya gönderme başarısız: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnPostRefreshProductsAsync()
        {
            try
            {
                var isAuthenticated = await _googleShoppingService.AuthenticateAsync();
                if (!isAuthenticated)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Google Shopping API bağlantısı kurulamadı!"
                    });
                }

                GoogleProducts = await _googleShoppingService.GetAllProductsAsync();
                ApprovalStatus = await _googleShoppingService.GetProductApprovalStatusAsync();
                
                return new JsonResult(new
                {
                    success = true,
                    message = $"{GoogleProducts.Count} ürün başarıyla yenilendi!",
                    productCount = GoogleProducts.Count,
                    status = ApprovalStatus
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Ürün listesi yenilenemedi: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnPostCheckStatusAsync()
        {
            try
            {
                var isAuthenticated = await _googleShoppingService.AuthenticateAsync();
                if (!isAuthenticated)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Google Shopping API bağlantısı kurulamadı!"
                    });
                }

                var status = await _googleShoppingService.GetProductApprovalStatusAsync();
                await _googleShoppingService.CheckProductStatusesAsync();
                
                return new JsonResult(new
                {
                    success = true,
                    message = "Ürün durumu kontrol edildi!",
                    status = status
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Durum kontrolü başarısız: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnPostBulkDeleteProductsAsync([FromBody] List<string> productIds)
        {
            try
            {
                if (productIds == null || productIds.Count == 0)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Silinecek ürün seçilmedi!"
                    });
                }

                var isAuthenticated = await _googleShoppingService.AuthenticateAsync();
                if (!isAuthenticated)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Google Shopping API bağlantısı kurulamadı!"
                    });
                }

                var success = await _googleShoppingService.BatchDeleteProductsAsync(productIds);
                
                if (success)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        message = $"{productIds.Count} ürün başarıyla Google Shopping'dan silindi!"
                    });
                }
                else
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Ürün silme işlemi başarısız!"
                    });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Toplu silme başarısız: {ex.Message}"
                });
            }
        }

    }
}
