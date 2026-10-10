using Microsoft.AspNetCore.Mvc;
using BalonPark.Attributes;
using BalonPark.Data;
using BalonPark.Services;
using BalonPark.Services.CatalogSync;

namespace BalonPark.Controllers
{
    [ApiController]
    [Route("api/category-order")]
    [RequireAdminSession]
    public class CategoryOrderController(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        ProductRepository productRepository,
        ICacheService cacheService,
        ICatalogSyncPublisher catalogSync,
        ILogger<CategoryOrderController> logger) : ControllerBase
    {

        /// <summary>
        /// Kategorileri yeniden sıralar
        /// </summary>
        [HttpPost("categories/reorder")]
        public async Task<IActionResult> ReorderCategories([FromBody] List<CategoryOrderDto> categories)
        {
            try
            {
                if (categories == null || !categories.Any())
                {
                    return BadRequest(new { success = false, message = "Kategori listesi boş olamaz" });
                }

                // Dictionary'ye dönüştür (categoryId -> displayOrder)
                var orderMap = categories.ToDictionary(c => c.Id, c => c.DisplayOrder);

                var success = await categoryRepository.ReorderCategoriesAsync(orderMap);

                if (success)
                {
                    // Cache'i temizle
                    await cacheService.InvalidateCategoriesAsync();
                    
                    logger.LogInformation("Kategoriler başarıyla yeniden sıralandı. Toplam: {Count}", categories.Count);
                    return Ok(new { success = true, message = $"{categories.Count} kategori başarıyla sıralandı" });
                }

                return BadRequest(new { success = false, message = "Sıralama işlemi başarısız" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kategori sıralama hatası");
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Alt kategorileri yeniden sıralar
        /// </summary>
        [HttpPost("subcategories/reorder")]
        public async Task<IActionResult> ReorderSubCategories([FromBody] List<CategoryOrderDto> subCategories)
        {
            try
            {
                if (subCategories == null || !subCategories.Any())
                {
                    return BadRequest(new { success = false, message = "Alt kategori listesi boş olamaz" });
                }

                // Dictionary'ye dönüştür (subCategoryId -> displayOrder)
                var orderMap = subCategories.ToDictionary(sc => sc.Id, sc => sc.DisplayOrder);

                var success = await subCategoryRepository.ReorderSubCategoriesAsync(orderMap);

                if (success)
                {
                    // Cache'i temizle
                    await cacheService.InvalidateSubCategoriesAsync();
                    
                    logger.LogInformation("Alt kategoriler başarıyla yeniden sıralandı. Toplam: {Count}", subCategories.Count);
                    return Ok(new { success = true, message = $"{subCategories.Count} alt kategori başarıyla sıralandı" });
                }

                return BadRequest(new { success = false, message = "Sıralama işlemi başarısız" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Alt kategori sıralama hatası");
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tek bir kategorinin sırasını günceller
        /// </summary>
        [HttpPut("categories/{id}/order")]
        public async Task<IActionResult> UpdateCategoryOrder(int id, [FromBody] UpdateOrderDto dto)
        {
            try
            {
                var success = await categoryRepository.UpdateDisplayOrderAsync(id, dto.DisplayOrder);

                if (success)
                {
                    // Cache'i temizle
                    await cacheService.InvalidateCategoriesAsync();
                    
                    return Ok(new { success = true, message = "Kategori sırası güncellendi" });
                }

                return NotFound(new { success = false, message = "Kategori bulunamadı" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kategori sırası güncelleme hatası: {Id}", id);
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tek bir alt kategorinin sırasını günceller
        /// </summary>
        [HttpPut("subcategories/{id}/order")]
        public async Task<IActionResult> UpdateSubCategoryOrder(int id, [FromBody] UpdateOrderDto dto)
        {
            try
            {
                var success = await subCategoryRepository.UpdateDisplayOrderAsync(id, dto.DisplayOrder);

                if (success)
                {
                    // Cache'i temizle
                    await cacheService.InvalidateSubCategoriesAsync();
                    
                    return Ok(new { success = true, message = "Alt kategori sırası güncellendi" });
                }

                return NotFound(new { success = false, message = "Alt kategori bulunamadı" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Alt kategori sırası güncelleme hatası: {Id}", id);
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Görünen ürünleri yeni sıraya koyar. Sıra numaralarını sunucu, mevcut yerlere göre hesaplar.
        /// </summary>
        [HttpPost("products/reorder")]
        public async Task<IActionResult> ReorderProducts([FromBody] List<int>? ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                    return BadRequest(new { success = false, message = "Ürün listesi boş olamaz" });

                if (ids.Count > 100)
                    return BadRequest(new { success = false, message = "Tek seferde en fazla 100 ürün sıralanabilir" });

                if (ids.Any(id => id <= 0))
                    return BadRequest(new { success = false, message = "Geçersiz ürün numarası" });

                var updates = await productRepository.ReorderVisibleProductsAsync(ids);
                foreach (var update in updates)
                    catalogSync.PublishUpsert(CatalogSyncEntityType.Product, update.Id);

                logger.LogInformation("Ürün sırası güncellendi. İstek: {Requested}, değişen: {Changed}", ids.Count, updates.Count);
                return Ok(new
                {
                    success = true,
                    message = updates.Count == 0 ? "Sıra zaten bu şekilde." : "Sıralama kaydedildi.",
                    orders = updates.Select(update => new { id = update.Id, displayOrder = update.DisplayOrder })
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ürün sıralama hatası");
                return StatusCode(500, new { success = false, message = "Sıralama kaydedilemedi." });
            }
        }
    }

    // DTO sınıfları
    public class CategoryOrderDto
    {
        public int Id { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class UpdateOrderDto
    {
        public int DisplayOrder { get; set; }
    }
}

