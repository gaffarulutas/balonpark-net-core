using Microsoft.AspNetCore.Mvc;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Controllers
{
    [ApiController]
    [Route("admin/googleshopping")]
    public class GoogleShoppingController(
        IGoogleShoppingService googleShoppingService,
        ILogger<GoogleShoppingController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Keys dosyasının konumunu ve erişilebilirliğini kontrol eder
        /// </summary>
        [HttpGet("debug-keys")]
        public IActionResult DebugKeys()
        {
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                var possiblePaths = new[]
                {
                    Path.Combine(currentDir, "Keys", "balonpark-c4c1d4e11838.json"),
                    Path.Combine(baseDir, "Keys", "balonpark-c4c1d4e11838.json"),
                    Path.Combine(baseDir, "bin", "Keys", "balonpark-c4c1d4e11838.json"),
                    Path.Combine(currentDir, "bin", "Keys", "balonpark-c4c1d4e11838.json"),
                    Path.Combine(baseDir, "..", "Keys", "balonpark-c4c1d4e11838.json"),
                    Path.Combine(currentDir, "..", "Keys", "balonpark-c4c1d4e11838.json")
                };

                var debugInfo = new
                {
                    CurrentDirectory = currentDir,
                    BaseDirectory = baseDir,
                    PathsChecked = possiblePaths.Select(p => new
                    {
                        Path = p,
                        Exists = System.IO.File.Exists(p),
                        DirectoryExists = Directory.Exists(System.IO.Path.GetDirectoryName(p))
                    }).ToArray(),
                    AllDirectoriesInBase = Directory.GetDirectories(baseDir).Select(Path.GetFileName).ToArray(),
                    AllFilesInBase = Directory.GetFiles(baseDir).Select(Path.GetFileName).ToArray()
                };

                return Ok(debugInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Debug failed: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Google Shopping API bağlantısını test eder
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var isAuthenticated = await googleShoppingService.AuthenticateAsync();
                
                return Ok(new
                {
                    success = isAuthenticated,
                    message = isAuthenticated ? "Google Shopping API bağlantısı başarılı!" : "Google Shopping API bağlantısı başarısız!",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Google Shopping API bağlantı testi başarısız");
                return BadRequest(new
                {
                    success = false,
                    message = $"Bağlantı testi başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Tüm ürünleri Google Shopping formatına dönüştürür
        /// </summary>
        [HttpGet("convert-products")]
        public async Task<IActionResult> ConvertProducts()
        {
            try
            {
                var googleProducts = await googleShoppingService.ConvertProductsToGoogleShoppingFormatAsync();
                
                return Ok(new
                {
                    success = true,
                    message = $"{googleProducts.Count} ürün Google Shopping formatına dönüştürüldü",
                    data = googleProducts,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ürün dönüştürme başarısız");
                return BadRequest(new
                {
                    success = false,
                    message = $"Ürün dönüştürme başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Tek bir ürünü Google Shopping'a ekler
        /// </summary>
        [HttpPost("create-product")]
        public async Task<IActionResult> CreateProduct([FromBody] GoogleShoppingProduct product)
        {
            try
            {
                var productId = await googleShoppingService.CreateProductAsync(product);
                
                return Ok(new
                {
                    success = !string.IsNullOrEmpty(productId),
                    message = !string.IsNullOrEmpty(productId) ? "Ürün başarıyla oluşturuldu!" : "Ürün oluşturulamadı!",
                    productId = productId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ürün oluşturma başarısız: {ProductId}", product.Id);
                return BadRequest(new
                {
                    success = false,
                    message = $"Ürün oluşturma başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Tek bir ürünü günceller
        /// </summary>
        [HttpPut("update-product/{productId}")]
        public async Task<IActionResult> UpdateProduct(string productId, [FromBody] GoogleShoppingProduct product)
        {
            try
            {
                product.Id = productId;
                var result = await googleShoppingService.UpdateProductAsync(product);
                
                return Ok(new
                {
                    success = !string.IsNullOrEmpty(result),
                    message = !string.IsNullOrEmpty(result) ? "Ürün başarıyla güncellendi!" : "Ürün güncellenemedi!",
                    productId = result,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ürün güncelleme başarısız: {ProductId}", productId);
                return BadRequest(new
                {
                    success = false,
                    message = $"Ürün güncelleme başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Tek bir ürünü siler
        /// </summary>
        [HttpDelete("delete-product/{productId}")]
        public async Task<IActionResult> DeleteProduct(string productId)
        {
            try
            {
                var success = await googleShoppingService.DeleteProductAsync(productId);
                
                return Ok(new
                {
                    success = success,
                    message = success ? "Ürün başarıyla silindi!" : "Ürün silinemedi!",
                    productId = productId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ürün silme başarısız: {ProductId}", productId);
                return BadRequest(new
                {
                    success = false,
                    message = $"Ürün silme başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Google Shopping'daki tüm ürünleri getirir
        /// </summary>
        [HttpGet("get-all-products")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await googleShoppingService.GetAllProductsAsync();
                
                return Ok(new
                {
                    success = true,
                    message = $"{products.Count} ürün başarıyla getirildi",
                    data = products,
                    count = products.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Google Shopping ürünleri getirme başarısız");
                return BadRequest(new
                {
                    success = false,
                    message = $"Ürünler getirilemedi: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Belirli bir ürünü getirir
        /// </summary>
        [HttpGet("get-product/{productId}")]
        public async Task<IActionResult> GetProduct(string productId)
        {
            try
            {
                var product = await googleShoppingService.GetProductAsync(productId);
                
                if (product == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Ürün bulunamadı",
                        productId = productId,
                        timestamp = DateTime.UtcNow
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Ürün başarıyla getirildi",
                    data = product,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ürün getirme başarısız: {ProductId}", productId);
                return BadRequest(new
                {
                    success = false,
                    message = $"Ürün getirilemedi: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Toplu ürün ekleme
        /// </summary>
        [HttpPost("batch-insert")]
        public async Task<IActionResult> BatchInsertProducts([FromBody] List<GoogleShoppingProduct> products)
        {
            try
            {
                var success = await googleShoppingService.BatchInsertProductsAsync(products);
                
                return Ok(new
                {
                    success = success,
                    message = success ? $"{products.Count} ürün toplu olarak eklendi!" : "Toplu ürün ekleme başarısız!",
                    count = products.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Toplu ürün ekleme başarısız");
                return BadRequest(new
                {
                    success = false,
                    message = $"Toplu ürün ekleme başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Toplu ürün güncelleme
        /// </summary>
        [HttpPut("batch-update")]
        public async Task<IActionResult> BatchUpdateProducts([FromBody] List<GoogleShoppingProduct> products)
        {
            try
            {
                var success = await googleShoppingService.BatchUpdateProductsAsync(products);
                
                return Ok(new
                {
                    success = success,
                    message = success ? $"{products.Count} ürün toplu olarak güncellendi!" : "Toplu ürün güncelleme başarısız!",
                    count = products.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Toplu ürün güncelleme başarısız");
                return BadRequest(new
                {
                    success = false,
                    message = $"Toplu ürün güncelleme başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Toplu ürün silme
        /// </summary>
        [HttpDelete("batch-delete")]
        public async Task<IActionResult> BatchDeleteProducts([FromBody] List<string> productIds)
        {
            try
            {
                var success = await googleShoppingService.BatchDeleteProductsAsync(productIds);
                
                return Ok(new
                {
                    success = success,
                    message = success ? $"{productIds.Count} ürün toplu olarak silindi!" : "Toplu ürün silme başarısız!",
                    count = productIds.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Toplu ürün silme başarısız");
                return BadRequest(new
                {
                    success = false,
                    message = $"Toplu ürün silme başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Tüm ürünleri senkronize eder (ekleme/güncelleme)
        /// </summary>
        [HttpPost("sync-all-products")]
        public async Task<IActionResult> SyncAllProducts()
        {
            try
            {
                var isAuthenticated = await googleShoppingService.AuthenticateAsync();
                if (!isAuthenticated)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Google Shopping API bağlantısı kurulamadı!",
                        timestamp = DateTime.UtcNow
                    });
                }

                var googleProducts = await googleShoppingService.ConvertProductsToGoogleShoppingFormatAsync();
                var success = await googleShoppingService.BatchInsertProductsAsync(googleProducts);
                
                return Ok(new
                {
                    success = success,
                    message = success ? $"{googleProducts.Count} ürün başarıyla senkronize edildi!" : "Ürün senkronizasyonu başarısız!",
                    count = googleProducts.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ürün senkronizasyonu başarısız");
                return BadRequest(new
                {
                    success = false,
                    message = $"Ürün senkronizasyonu başarısız: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
